using System.Text.Json;
using System.Text.Json.Serialization;
using SbmFizikusToMqtt.Infrastructure.Sbm.Json;

namespace SbmFizikusToMqtt.Infrastructure.Sbm.Models;

internal sealed record SbmTokenResponse
{
    [JsonPropertyName("token")] public required string Token { get; init; }

    [JsonPropertyName("expiration")] public required DateTimeOffset Expiration { get; init; }
}

/// <summary>
///     Positional: <c>[buildingId, country, postalCode, city, street, outdoorTemp, outdoorHum, name]</c>. Only the id
///     is required: the list is also read when outdoor weather is off, to resolve the apartment.
/// </summary>
[JsonConverter(typeof(SbmBuildingAccessRightConverter))]
internal sealed record SbmBuildingAccessRight
{
    public required int BuildingId { get; init; }

    /// <summary>Null when the building has no outdoor sensor value.</summary>
    public double? OutdoorTemperature { get; init; }

    /// <summary>Null when the building has no outdoor sensor value.</summary>
    public double? OutdoorHumidity { get; init; }

    /// <summary>Only used in log messages.</summary>
    public string? Name { get; init; }
}

/// <summary>Positional: <c>[buildingId, apartmentId, name, isOnline]</c>. Only the ids are required.</summary>
[JsonConverter(typeof(SbmApartmentAccessRightConverter))]
internal sealed record SbmApartmentAccessRight
{
    public required int ApartmentId { get; init; }

    /// <summary>Only used in log messages.</summary>
    public string? Name { get; init; }
}

internal sealed record SbmApartmentDataResponse
{
    [JsonPropertyName("operationMode")] public required int OperationMode { get; init; }

    [JsonPropertyName("lastStateUpdate")] public DateTimeOffset? LastStateUpdate { get; init; }

    [JsonPropertyName("communicationActiveRelayModule")]
    public required bool CommunicationActiveRelayModule { get; init; }

    [JsonPropertyName("communicationActiveThermostats")]
    public required bool CommunicationActiveThermostats { get; init; }

    [JsonPropertyName("thermostats")] public required IReadOnlyList<SbmThermostat> Thermostats { get; init; }
}

/// <summary>
///     Only the id is required. A thermostat that has not reported yet (e.g. just paired, or with a dead battery) may
///     have missing values; they must not fail the other thermostats' state.
/// </summary>
internal sealed record SbmThermostat
{
    [JsonPropertyName("ID")] public required int Id { get; init; }

    [JsonPropertyName("name")] public string? Name { get; init; }

    [JsonPropertyName("active")] public bool? Active { get; init; }

    [JsonPropertyName("measuredTempDegC")] public double? MeasuredTemperature { get; init; }

    [JsonPropertyName("measuredHumPerc")] public double? MeasuredHumidity { get; init; }

    [JsonPropertyName("temperatureSetpointDegC")] public double? TemperatureSetpoint { get; init; }

    [JsonPropertyName("lastStateUpdate")] public DateTimeOffset? LastStateUpdate { get; init; }
}

internal sealed record SbmThermostatSettingsResponse
{
    [JsonPropertyName("thermostat_id")] public required int ThermostatId { get; init; }
}

internal sealed class SbmBuildingAccessRightConverter : PositionalArrayConverter<SbmBuildingAccessRight>
{
    protected override int MinimumLength => 1;

    protected override SbmBuildingAccessRight Map(IReadOnlyList<JsonElement> elements)
    {
        return new SbmBuildingAccessRight
        {
            BuildingId = elements[0].GetInt32(),
            OutdoorTemperature = OptionalDouble(elements, 5),
            OutdoorHumidity = OptionalDouble(elements, 6),
            Name = OptionalString(elements, 7)
        };
    }
}

internal sealed class SbmApartmentAccessRightConverter : PositionalArrayConverter<SbmApartmentAccessRight>
{
    protected override int MinimumLength => 2;

    protected override SbmApartmentAccessRight Map(IReadOnlyList<JsonElement> elements)
    {
        return new SbmApartmentAccessRight
        {
            ApartmentId = elements[1].GetInt32(),
            Name = OptionalString(elements, 2)
        };
    }
}