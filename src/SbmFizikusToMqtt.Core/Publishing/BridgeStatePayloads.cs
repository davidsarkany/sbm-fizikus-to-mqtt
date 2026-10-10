namespace SbmFizikusToMqtt.Core.Publishing;

/// <summary>
///     Payloads of the retained <c>bridge/state</c> topic, which signals whether the bridge process is alive.
///     Kept byte-for-byte identical to v2.
/// </summary>
public static class BridgeStatePayloads
{
    public const string Online = "{\"state\": \"online\"}";
    public const string Offline = "{\"state\": \"offline\"}";
}