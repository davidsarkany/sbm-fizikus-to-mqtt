namespace SbmFizikusToMqtt.MqttConnector.Models;

/// <summary>
///     JSON payloads published to the bridge state topic. Kept as constants to preserve the exact
///     wire format expected by Home Assistant and existing consumers.
/// </summary>
internal static class BridgeStatePayloads
{
    public const string Online = "{\"state\": \"online\"}";
    public const string Offline = "{\"state\": \"offline\"}";
}