using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SbmFizikusToMqtt.SbmConnector.Models.Requests;

internal sealed record SbmChangeTemperatureRequest
{
    [SetsRequiredMembers]
    public SbmChangeTemperatureRequest(int thermostatId, double temperature, string jwtToken)
    {
        Operation = "update_thermostat_settings";
        Payload = new PayloadData
        {
            ThermostatId = thermostatId,
            TempSetpointDegC = temperature,
            JwtToken = jwtToken
        };
    }

    [JsonPropertyName("operation")] public required string Operation { get; init; }

    [JsonPropertyName("payload")] public required PayloadData Payload { get; init; }

    internal sealed record PayloadData
    {
        [JsonPropertyName("thermostat_id")] public required int ThermostatId { get; init; }

        [JsonPropertyName("temp_setpoint_degC")]
        public required double TempSetpointDegC { get; init; }

        [JsonPropertyName("jwt_token")] public required string JwtToken { get; init; }
    }
}