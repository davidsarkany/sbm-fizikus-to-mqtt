namespace SbmFizikusToMqtt.Core.Bridge;

/// <summary>Work item for <see cref="BridgeProcessor" />. All commands are processed one at a time.</summary>
public abstract record BridgeCommand;

/// <summary>Periodic poll: read SBM and publish the current state.</summary>
public sealed record PollRequested : BridgeCommand;

/// <summary>The MQTT connection was (re-)established; state and discovery must be published again.</summary>
public sealed record MqttConnected : BridgeCommand;

/// <summary>
///     A message arrived on one of the bridge's subscriptions. <paramref name="Retained" /> is true when the broker
///     delivered a stored retained message on subscribe instead of a new one.
/// </summary>
public sealed record MqttMessageReceived(string Topic, string Payload, bool Retained = false) : BridgeCommand;