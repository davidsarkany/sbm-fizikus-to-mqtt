using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;

internal sealed record ClimateAutoDiscovery
{
    [JsonPropertyName("action_template")] public required string ActionTemplate { get; init; }

    [JsonPropertyName("action_topic")] public required string ActionTopic { get; init; }

    [JsonPropertyName("availability")] public required IEnumerable<Availability> Availability { get; init; }

    [JsonPropertyName("current_humidity_template")]
    public required string CurrentHumidityTemplate { get; init; }

    [JsonPropertyName("current_humidity_topic")]
    public required string CurrentHumidityTopic { get; init; }

    [JsonPropertyName("current_temperature_template")]
    public required string CurrentTemperatureTemplate { get; init; }

    [JsonPropertyName("current_temperature_topic")]
    public required string CurrentTemperatureTopic { get; init; }

    [JsonPropertyName("temp_step")]
    public required double TemperatureStep { get; init; }

    [JsonPropertyName("precision")]
    public required double Precision { get; init; }

    [JsonPropertyName("unique_id")] public required string UniqueId { get; init; }

    [JsonPropertyName("temperature_unit")] public required string TemperatureUnit { get; init; }

    [JsonPropertyName("modes")] public required IEnumerable<string> Modes { get; init; }

    [JsonPropertyName("mode_state_template")]
    public required string ModeStateTemplate { get; init; }

    [JsonPropertyName("mode_state_topic")] public required string ModeStateTopic { get; init; }

    [JsonPropertyName("temperature_state_template")]
    public required string TemperatureStateTemplate { get; init; }

    [JsonPropertyName("temperature_state_topic")]
    public required string TemperatureStateTopic { get; init; }

    [JsonPropertyName("temperature_command_template")]
    public required string TemperatureCommandTemplate { get; init; }

    [JsonPropertyName("temperature_command_topic")]
    public required string TemperatureCommandTopic { get; init; }

    [JsonPropertyName("name")] public required string? Name { get; init; }
}