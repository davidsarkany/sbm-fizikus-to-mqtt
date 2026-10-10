using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.Infrastructure.Sbm.Models;

/// <summary>Every SBM call is <c>PUT /frontend</c> with an operation name and an operation-specific payload.</summary>
internal sealed record SbmRequest<TPayload>
{
    [JsonPropertyName("operation")] public required string Operation { get; init; }

    [JsonPropertyName("payload")] public required TPayload Payload { get; init; }
}

internal sealed record SbmLoginPayload
{
    [JsonPropertyName("username_or_email")] public required string UsernameOrEmail { get; init; }

    [JsonPropertyName("pwhash")] public required string PasswordHash { get; init; }
}

internal sealed record SbmTokenPayload
{
    [JsonPropertyName("jwt_token")] public required string JwtToken { get; init; }
}

internal sealed record SbmApartmentAccessRightsPayload
{
    /// <summary>SBM expects the building id as a string here.</summary>
    [JsonPropertyName("building_id")] public required string BuildingId { get; init; }

    [JsonPropertyName("jwt_token")] public required string JwtToken { get; init; }
}

internal sealed record SbmApartmentDataPayload
{
    [JsonPropertyName("apartment_id")] public required int ApartmentId { get; init; }

    [JsonPropertyName("jwt_token")] public required string JwtToken { get; init; }
}

internal sealed record SbmThermostatSettingsPayload
{
    [JsonPropertyName("thermostat_id")] public required int ThermostatId { get; init; }

    [JsonPropertyName("temp_setpoint_degC")] public required double TemperatureSetpoint { get; init; }

    [JsonPropertyName("jwt_token")] public required string JwtToken { get; init; }
}