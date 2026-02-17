using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.MqttConnector.Models;

internal sealed record MqttThermostat
{
    [JsonPropertyName("id")] public required int Id { get; init; }

    [JsonPropertyName("name")] public required string? Name { get; init; }

    [JsonPropertyName("temperature")] public required double Temperature { get; init; }

    [JsonPropertyName("humidity")] public required double Humidity { get; init; }

    [JsonPropertyName("target_temperature")]
    public required double TargetTemperature { get; init; }

    [JsonPropertyName("system_mode")] public required string SystemMode { get; init; }

    [JsonPropertyName("last_update")] public required DateTimeOffset LastUpdate { get; init; }
}