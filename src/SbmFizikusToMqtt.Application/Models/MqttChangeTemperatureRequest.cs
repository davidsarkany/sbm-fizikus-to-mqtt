using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.Application.Models;

internal sealed record MqttChangeTemperatureRequest
{
    [JsonPropertyName("id")] public required int Id { get; init; }

    [JsonPropertyName("value")] public required double Value { get; init; }
}