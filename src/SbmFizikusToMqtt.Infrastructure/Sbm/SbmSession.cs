using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Infrastructure.Sbm.Models;

namespace SbmFizikusToMqtt.Infrastructure.Sbm;

/// <summary>
///     <see cref="ISbmGateway" /> backed by the SBM API. It caches the access token and the apartment id, and it
///     logs in again once when SBM rejects the token. When SBM rejects the login itself, further logins are paused
///     with an exponential backoff (<see cref="InitialLoginPause" /> up to <see cref="MaximumLoginPause" />).
///     Not thread-safe: <c>BridgeProcessor</c> calls it serially.
/// </summary>
internal sealed class SbmSession(
    SbmClient client,
    IOptions<SbmOptions> options,
    TimeProvider timeProvider,
    ILogger<SbmSession> logger) : ISbmGateway
{
    internal static readonly TimeSpan InitialLoginPause = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan MaximumLoginPause = TimeSpan.FromHours(1);

    private static readonly TimeSpan TokenRefreshMargin = TimeSpan.FromSeconds(30);

    private readonly SbmOptions _options = options.Value;
    private int? _apartmentId;
    private int _rejectedLogins;
    private TimeSpan _loginPause = InitialLoginPause;
    private DateTimeOffset? _loginPausedUntil;
    private SbmTokenResponse? _token;

    public Task<Apartment> GetApartmentAsync(CancellationToken cancellationToken)
    {
        return WithAuthenticationRetryAsync(token => LoadApartmentAsync(token, cancellationToken), cancellationToken);
    }

    public Task SetTargetTemperatureAsync(int thermostatId, double temperature, CancellationToken cancellationToken)
    {
        return WithAuthenticationRetryAsync(
            token => client.UpdateThermostatSettingsAsync(thermostatId, temperature, token, cancellationToken),
            cancellationToken);
    }

    private async Task<T> WithAuthenticationRetryAsync<T>(Func<string, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        try
        {
            return await operation(token);
        }
        catch (SbmApiException ex) when (ex.IsAuthenticationFailure)
        {
            // The token may have been revoked or the account's access changed: start a fresh session once.
            logger.LogWarning("SBM rejected the session ({StatusCode}), logging in again", (int)ex.StatusCode);
            _token = null;
            _apartmentId = null;
            return await operation(await GetTokenAsync(cancellationToken));
        }
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (_token is null || _token.Expiration <= now + TokenRefreshMargin)
        {
            if (_loginPausedUntil > now)
            {
                throw new SbmLoginPausedException(
                    $"SBM login is paused until {_loginPausedUntil:u} after {_rejectedLogins} rejected attempts; " +
                    "check SbmConnector__Username and SbmConnector__Password");
            }

            try
            {
                _token = await client.LoginAsync(_options.Username, _options.Password, cancellationToken);
            }
            catch (SbmException ex) when (IsRejectedLogin(ex))
            {
                _rejectedLogins++;
                _loginPausedUntil = now + _loginPause;
                logger.LogError(
                    "SBM rejected the login ({RejectedLogins} in a row): {Reason}. Check SbmConnector__Username and " +
                    "SbmConnector__Password; the next attempt is at {NextAttempt}", _rejectedLogins, ex.Message,
                    _loginPausedUntil);
                _loginPause = _loginPause * 2 > MaximumLoginPause ? MaximumLoginPause : _loginPause * 2;
                throw;
            }

            if (_rejectedLogins > 0)
                logger.LogInformation("Logged in to SBM after {RejectedLogins} rejected attempts", _rejectedLogins);

            _rejectedLogins = 0;
            _loginPause = InitialLoginPause;
            _loginPausedUntil = null;
            logger.LogDebug("Logged in to SBM, token valid until {Expiration}", _token.Expiration);
        }

        return _token.Token;
    }

    /// <summary>
    ///     A login that SBM answered but refused (client error status, or a success status without a token). Network
    ///     errors and server errors are transient and are not paused, so the bridge recovers as soon as SBM does.
    /// </summary>
    private static bool IsRejectedLogin(SbmException exception)
    {
        return exception switch
        {
            SbmApiException api => (int)api.StatusCode is >= 400 and < 500 and not 408 and not 429,
            SbmInvalidResponseException => true,
            _ => false
        };
    }

    private async Task<Apartment> LoadApartmentAsync(string token, CancellationToken cancellationToken)
    {
        double? outdoorTemperature = null;
        double? outdoorHumidity = null;

        // Building access rights carry the outdoor weather, and are needed once to resolve the apartment.
        if (_options.OutdoorWeatherEnabled || _apartmentId is null)
        {
            var buildings = await client.GetBuildingAccessRightsAsync(token, cancellationToken);
            var building = buildings[0];

            if (_options.OutdoorWeatherEnabled)
            {
                outdoorTemperature = building.OutdoorTemperature;
                outdoorHumidity = building.OutdoorHumidity;
            }

            if (_apartmentId is null)
                _apartmentId = await ResolveApartmentIdAsync(buildings, token, cancellationToken);
        }

        var data = await client.GetApartmentDataAsync(_apartmentId.Value, token, cancellationToken);
        return data.ToApartment(outdoorTemperature, outdoorHumidity);
    }

    private async Task<int> ResolveApartmentIdAsync(IReadOnlyList<SbmBuildingAccessRight> buildings, string token,
        CancellationToken cancellationToken)
    {
        var building = buildings[0];
        var apartments = await client.GetApartmentAccessRightsAsync(building.BuildingId, token, cancellationToken);
        var apartment = apartments[0];

        if (buildings.Count > 1 || apartments.Count > 1)
        {
            logger.LogWarning(
                "The SBM account has access to {BuildingCount} buildings and {ApartmentCount} apartments; " +
                "only the first one ({ApartmentName}) is bridged", buildings.Count, apartments.Count, apartment.Name);
        }

        logger.LogInformation("Using SBM apartment {ApartmentName} ({ApartmentId}) in building {BuildingName}",
            apartment.Name, apartment.ApartmentId, building.Name);
        return apartment.ApartmentId;
    }
}