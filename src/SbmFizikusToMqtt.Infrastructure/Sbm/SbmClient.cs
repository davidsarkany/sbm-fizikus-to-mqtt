using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SbmFizikusToMqtt.Infrastructure.Sbm.Json;
using SbmFizikusToMqtt.Infrastructure.Sbm.Models;

namespace SbmFizikusToMqtt.Infrastructure.Sbm;

/// <summary>Thin typed wrapper over the SBM Fizikus HTTP API (one method per operation).</summary>
internal sealed class SbmClient(IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "SbmClient";

    private const string RequestUri = "/frontend";
    private const int MaxErrorBodyLength = 1000;

    public Task<SbmTokenResponse> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var request = new SbmRequest<SbmLoginPayload>
        {
            Operation = "get_jw_token",
            Payload = new SbmLoginPayload { UsernameOrEmail = username, PasswordHash = HashPassword(password) }
        };

        // The response contains the token, so it is never echoed into error messages.
        return SendAsync(request, SbmJsonContext.Default.SbmRequestSbmLoginPayload,
            SbmJsonContext.Default.SbmTokenResponse, false, cancellationToken);
    }

    public Task<IReadOnlyList<SbmBuildingAccessRight>> GetBuildingAccessRightsAsync(string token,
        CancellationToken cancellationToken)
    {
        var request = new SbmRequest<SbmTokenPayload>
        {
            Operation = "get_building_access_rights",
            Payload = new SbmTokenPayload { JwtToken = token }
        };

        return SendAsync(request, SbmJsonContext.Default.SbmRequestSbmTokenPayload,
            SbmJsonContext.Default.IReadOnlyListSbmBuildingAccessRight, true, cancellationToken,
            buildings => buildings.Count > 0 ? null : "SBM returned no buildings for the account");
    }

    public Task<IReadOnlyList<SbmApartmentAccessRight>> GetApartmentAccessRightsAsync(int buildingId,
        string token, CancellationToken cancellationToken)
    {
        var request = new SbmRequest<SbmApartmentAccessRightsPayload>
        {
            Operation = "get_apartment_access_rights",
            Payload = new SbmApartmentAccessRightsPayload
            {
                BuildingId = buildingId.ToString(CultureInfo.InvariantCulture),
                JwtToken = token
            }
        };

        return SendAsync(request, SbmJsonContext.Default.SbmRequestSbmApartmentAccessRightsPayload,
            SbmJsonContext.Default.IReadOnlyListSbmApartmentAccessRight, true, cancellationToken,
            apartments => apartments.Count > 0 ? null : $"SBM returned no apartments for building {buildingId}");
    }

    public Task<SbmApartmentDataResponse> GetApartmentDataAsync(int apartmentId, string token,
        CancellationToken cancellationToken)
    {
        var request = new SbmRequest<SbmApartmentDataPayload>
        {
            Operation = "get_apartment_data",
            Payload = new SbmApartmentDataPayload { ApartmentId = apartmentId, JwtToken = token }
        };

        return SendAsync(request, SbmJsonContext.Default.SbmRequestSbmApartmentDataPayload,
            SbmJsonContext.Default.SbmApartmentDataResponse, true, cancellationToken);
    }

    public Task<SbmThermostatSettingsResponse> UpdateThermostatSettingsAsync(int thermostatId, double temperature,
        string token, CancellationToken cancellationToken)
    {
        var request = new SbmRequest<SbmThermostatSettingsPayload>
        {
            Operation = "update_thermostat_settings",
            Payload = new SbmThermostatSettingsPayload
            {
                ThermostatId = thermostatId,
                TemperatureSetpoint = temperature,
                JwtToken = token
            }
        };

        return SendAsync(request, SbmJsonContext.Default.SbmRequestSbmThermostatSettingsPayload,
            SbmJsonContext.Default.SbmThermostatSettingsResponse, true, cancellationToken);
    }

    /// <summary>SBM expects the lowercase hex SHA-256 hash of the UTF-8 password.</summary>
    internal static string HashPassword(string password)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
    }

    /// <summary>
    ///     Sends one operation inside an <c>SBM {operation}</c> span. The HTTP span created by the HttpClient
    ///     instrumentation becomes its child, so traces show which call each <c>PUT /frontend</c> was.
    /// </summary>
    private async Task<TResponse> SendAsync<TPayload, TResponse>(
        SbmRequest<TPayload> request,
        JsonTypeInfo<SbmRequest<TPayload>> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        bool includeBodyInErrors,
        CancellationToken cancellationToken,
        Func<TResponse, string?>? validate = null) where TResponse : class
    {
        using var activity = SbmTelemetry.ActivitySource.StartActivity($"SBM {request.Operation}");
        activity?.SetTag(SbmTelemetry.OperationTag, request.Operation);

        try
        {
            var httpClient = httpClientFactory.CreateClient(HttpClientName);
            // Buffered with Content-Length like v2; the API gateway behaviour with chunked requests is untested.
            using var content = new StringContent(JsonSerializer.Serialize(request, requestTypeInfo), Encoding.UTF8,
                "application/json");
            using var response = await httpClient.PutAsync(RequestUri, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new SbmApiException(response.StatusCode,
                    $"SBM API request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}): " +
                    (includeBodyInErrors ? Truncate(body) : "<redacted>"));
            }

            var result = Deserialize(body, responseTypeInfo, includeBodyInErrors);
            var validationError = validate?.Invoke(result);
            return validationError is null ? result : throw new SbmInvalidResponseException(validationError);
        }
        catch (Exception ex) when (activity is not null && ex is not OperationCanceledException)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.AddException(ex);
            throw;
        }
    }

    private static TResponse Deserialize<TResponse>(string body, JsonTypeInfo<TResponse> responseTypeInfo,
        bool includeBodyInErrors) where TResponse : class
    {
        var bodyForErrors = includeBodyInErrors ? Truncate(body) : "<redacted>";
        try
        {
            return JsonSerializer.Deserialize(body, responseTypeInfo)
                   ?? throw new SbmInvalidResponseException($"SBM returned an empty response: {bodyForErrors}");
        }
        catch (JsonException ex)
        {
            throw new SbmInvalidResponseException($"SBM returned an invalid response: {bodyForErrors}", ex);
        }
    }

    private static string Truncate(string body)
    {
        return body.Length <= MaxErrorBodyLength ? body : string.Concat(body.AsSpan(0, MaxErrorBodyLength), "…");
    }
}