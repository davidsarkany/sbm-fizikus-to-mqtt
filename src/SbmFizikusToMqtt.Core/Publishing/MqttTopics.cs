using System.Globalization;
using System.Text.RegularExpressions;
using SbmFizikusToMqtt.Core.Options;

namespace SbmFizikusToMqtt.Core.Publishing;

/// <summary>The bridge's MQTT topic scheme.</summary>
public sealed partial class MqttTopics(TopicOptions options)
{
    private const string DevicesSegment = "devices";
    private const string SetSegment = "set";

    public string SbmTopic { get; } = options.SbmTopic;
    public string HomeAssistantTopic { get; } = options.HomeAssistantTopic;

    /// <summary>Discovery node id derived from <see cref="SbmTopic" />, restricted to characters HA accepts.</summary>
    public string NodeId { get; } = InvalidNodeIdCharacters().Replace(options.SbmTopic, "_");

    public string BridgeState => $"{SbmTopic}/bridge/state";
    public string ApartmentInfo => $"{SbmTopic}/apartment_info";
    public string DeviceSetFilter => $"{SbmTopic}/{DevicesSegment}/+/{SetSegment}";
    public string HomeAssistantStatus => $"{HomeAssistantTopic}/status";
    public string ApartmentDiscovery => $"{HomeAssistantTopic}/device/{NodeId}_apartment/config";

    public string Device(int thermostatId)
    {
        return $"{SbmTopic}/{DevicesSegment}/{thermostatId}";
    }

    public string DeviceSet(int thermostatId)
    {
        return $"{Device(thermostatId)}/{SetSegment}";
    }

    public string ThermostatDiscovery(int thermostatId)
    {
        return $"{HomeAssistantTopic}/device/{NodeId}_thermostat_{thermostatId}/config";
    }

    /// <summary>Parses <c>{SbmTopic}/devices/{id}/set</c> and returns the thermostat id.</summary>
    public bool TryParseDeviceSet(string topic, out int thermostatId)
    {
        thermostatId = 0;
        var prefix = $"{SbmTopic}/{DevicesSegment}/";
        var suffix = $"/{SetSegment}";
        if (!topic.StartsWith(prefix, StringComparison.Ordinal) || !topic.EndsWith(suffix, StringComparison.Ordinal)
                                                                || topic.Length <= prefix.Length + suffix.Length)
            return false;

        var id = topic.AsSpan(prefix.Length, topic.Length - prefix.Length - suffix.Length);
        return int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out thermostatId);
    }

    [GeneratedRegex("[^a-zA-Z0-9_-]")]
    private static partial Regex InvalidNodeIdCharacters();
}