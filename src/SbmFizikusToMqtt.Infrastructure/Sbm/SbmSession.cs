using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Infrastructure.Sbm.Models;

namespace SbmFizikusToMqtt.Infrastructure.Sbm;

/// <summary>
///     <see cref="ISbmGateway" /> backed by the SBM API. It caches the access token and the apartment id, and it
///     logs in again once when SBM rejects the token. Not thread-safe: <c>BridgeProcessor</c> calls it serially.
/// </summary>
internal sealed class SbmSession(
    SbmClient client,
    IOptions<SbmOptions> options,
    TimeProvider timeProvider,
    ILogger<SbmSession> logger) : ISbmGateway
{
    private static readonly TimeSpan TokenRefreshMargin = TimeSpan.FromSeconds(30);

    private readonly SbmOptions _options = options.Value;
    private int? _apartmentId;
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
        if (_token is null || _token.Expiration <= timeProvider.GetUtcNow() + TokenRefreshMargin)
        {
            _token = await client.LoginAsync(_options.Username, _options.Password, cancellationToken);
            logger.LogDebug("Logged in to SBM, token valid until {Expiration}", _token.Expiration);
        }

        return _token.Token;
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