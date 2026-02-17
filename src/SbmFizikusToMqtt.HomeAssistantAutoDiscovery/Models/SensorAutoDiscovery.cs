using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;

internal sealed record SensorAutoDiscovery
{
    [JsonPropertyName("availability")] public required IEnumerable<Availability> Availability { get; init; }

    [JsonPropertyName("device_class")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? DeviceClass { get; init; }

    [JsonPropertyName("entity_category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? EntityCategory { get; init; }

    [JsonPropertyName("name")] public required string? Name { get; init; }

    [JsonPropertyName("state_class")] public string? StateClass { get; init; }

    [JsonPropertyName("state_topic")] public string? StateTopic { get; init; }

    [JsonPropertyName("unique_id")] public required string UniqueId { get; init; }

    [JsonPropertyName("unit_of_measurement")]
    public string? UnitOfMeasurement { get; init; }

    [JsonPropertyName("value_template")] public required string ValueTemplate { get; init; }
}