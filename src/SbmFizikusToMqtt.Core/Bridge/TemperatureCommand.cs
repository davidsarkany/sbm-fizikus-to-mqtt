using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using SbmFizikusToMqtt.Core.Domain;

namespace SbmFizikusToMqtt.Core.Bridge;

/// <summary>A validated target temperature change received on <c>{SbmTopic}/devices/{id}/set</c>.</summary>
internal sealed record TemperatureCommand(int ThermostatId, double Temperature)
{
    /// <summary>
    ///     Parses the JSON payload <c>{"id": 1763, "value": 21.5}</c>. The id is optional, but when present it must
    ///     match the id in the topic. Property names are case-insensitive.
    /// </summary>
    public static bool TryParse(int topicThermostatId, string payload, [NotNullWhen(true)] out TemperatureCommand? command,
        out string error)
    {
        command = null;

        double? value = null;
        int? payloadId = null;
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "payload must be a JSON object";
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.Equals("value", StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetDouble(out var parsed))
                    {
                        error = "'value' must be a number";
                        return false;
                    }

                    value = parsed;
                }
                else if (property.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out var id))
                    {
                        error = "'id' must be an integer";
                        return false;
                    }

                    payloadId = id;
                }
            }
        }
        catch (JsonException)
        {
            error = "payload is not valid JSON";
            return false;
        }

        if (value is null)
        {
            error = "'value' is missing";
            return false;
        }

        if (payloadId is not null && payloadId != topicThermostatId)
        {
            error = $"payload id {payloadId} does not match topic id {topicThermostatId}";
            return false;
        }

        if (!TargetTemperatureLimits.IsValid(value.Value))
        {
            error = string.Create(CultureInfo.InvariantCulture,
                $"temperature must be between {TargetTemperatureLimits.Minimum} and {TargetTemperatureLimits.Maximum} in {TargetTemperatureLimits.Step} steps");
            return false;
        }

        command = new TemperatureCommand(topicThermostatId, value.Value);
        error = string.Empty;
        return true;
    }
}