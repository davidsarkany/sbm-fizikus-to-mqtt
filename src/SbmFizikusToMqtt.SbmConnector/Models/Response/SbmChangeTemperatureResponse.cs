using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.SbmConnector.Models.Response;

internal record SbmChangeTemperatureResponse
{
    [JsonPropertyName("message")] public required string Message { get; init; }

    [JsonPropertyName("thermostat_id")] public required int ThermostatId { get; init; }
}