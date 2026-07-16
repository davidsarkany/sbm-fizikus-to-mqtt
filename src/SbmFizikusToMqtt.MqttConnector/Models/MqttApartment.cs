using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.MqttConnector.Models;

internal sealed record MqttApartment
{
    [JsonPropertyName("system_mode")] public required string SystemMode { get; init; }

    [JsonPropertyName("last_update")] public required DateTimeOffset LastUpdate { get; init; }

    [JsonPropertyName("outdoor_temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? OutdoorTemperature { get; init; }

    [JsonPropertyName("outdoor_humidity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? OutdoorHumidity { get; init; }
}