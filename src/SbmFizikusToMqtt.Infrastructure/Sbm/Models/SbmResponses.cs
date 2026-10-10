using System.Text.Json;
using System.Text.Json.Serialization;
using SbmFizikusToMqtt.Infrastructure.Sbm.Json;

namespace SbmFizikusToMqtt.Infrastructure.Sbm.Models;

internal sealed record SbmTokenResponse
{
    [JsonPropertyName("token")] public required string Token { get; init; }

    [JsonPropertyName("expiration")] public required DateTimeOffset Expiration { get; init; }
}

/// <summary>Positional: <c>[buildingId, country, postalCode, city, street, outdoorTemp, outdoorHum, name]</c>.</summary>
[JsonConverter(typeof(SbmBuildingAccessRightConverter))]
internal sealed record SbmBuildingAccessRight
{
    public required int BuildingId { get; init; }
    public required double OutdoorTemperature { get; init; }
    public required double OutdoorHumidity { get; init; }
    public required string Name { get; init; }
}

/// <summary>Positional: <c>[buildingId, apartmentId, name, isOnline]</c>.</summary>
[JsonConverter(typeof(SbmApartmentAccessRightConverter))]
internal sealed record SbmApartmentAccessRight
{
    public required int BuildingId { get; init; }
    public required int ApartmentId { get; init; }
    public required string Name { get; init; }
    public required bool IsOnline { get; init; }
}

internal sealed record SbmApartmentDataResponse
{
    [JsonPropertyName("operationMode")] public required int OperationMode { get; init; }

    [JsonPropertyName("lastStateUpdate")] public required DateTimeOffset LastStateUpdate { get; init; }

    [JsonPropertyName("communicationActiveRelayModule")]
    public required bool CommunicationActiveRelayModule { get; init; }

    [JsonPropertyName("communicationActiveThermostats")]
    public required bool CommunicationActiveThermostats { get; init; }

    [JsonPropertyName("thermostats")] public required IReadOnlyList<SbmThermostat> Thermostats { get; init; }
}

internal sealed record SbmThermostat
{
    [JsonPropertyName("ID")] public required int Id { get; init; }

    [JsonPropertyName("name")] public required string? Name { get; init; }

    [JsonPropertyName("active")] public required bool Active { get; init; }

    [JsonPropertyName("measuredTempDegC")] public required double MeasuredTemperature { get; init; }

    [JsonPropertyName("measuredHumPerc")] public required double MeasuredHumidity { get; init; }

    [JsonPropertyName("temperatureSetpointDegC")] public required double TemperatureSetpoint { get; init; }

    [JsonPropertyName("dewPointDegC")] public required double DewPoint { get; init; }

    [JsonPropertyName("lastStateUpdate")] public required DateTimeOffset LastStateUpdate { get; init; }
}

internal sealed record SbmThermostatSettingsResponse
{
    [JsonPropertyName("thermostat_id")] public required int ThermostatId { get; init; }
}

internal sealed class SbmBuildingAccessRightConverter : PositionalArrayConverter<SbmBuildingAccessRight>
{
    protected override int Length => 8;

    protected override SbmBuildingAccessRight Map(IReadOnlyList<JsonElement> elements)
    {
        return new SbmBuildingAccessRight
        {
            BuildingId = elements[0].GetInt32(),
            OutdoorTemperature = elements[5].GetDouble(),
            OutdoorHumidity = elements[6].GetDouble(),
            Name = RequiredString(elements[7])
        };
    }
}

internal sealed class SbmApartmentAccessRightConverter : PositionalArrayConverter<SbmApartmentAccessRight>
{
    protected override int Length => 4;

    protected override SbmApartmentAccessRight Map(IReadOnlyList<JsonElement> elements)
    {
        return new SbmApartmentAccessRight
        {
            BuildingId = elements[0].GetInt32(),
            ApartmentId = elements[1].GetInt32(),
            Name = RequiredString(elements[2]),
            IsOnline = elements[3].GetBoolean()
        };
    }
}