using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;

internal sealed record Availability
{
    [JsonPropertyName("topic")] public required string Topic { get; init; }

    [JsonPropertyName("value_template")] public required string ValueTemplate { get; init; }
}