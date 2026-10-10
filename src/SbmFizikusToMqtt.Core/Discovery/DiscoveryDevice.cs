using System.Text.Json.Nodes;

namespace SbmFizikusToMqtt.Core.Discovery;

/// <summary>One Home Assistant entity of a discovered device. <see cref="Config" /> excludes the platform key.</summary>
internal sealed record DiscoveryComponent(string Key, string Platform, JsonObject Config);

/// <summary>A component that is switched off in the configuration and must be removed from the device.</summary>
internal sealed record DisabledDiscoveryComponent(string Key, string Platform);

/// <summary>A Home Assistant device published with device-based discovery on <see cref="Topic" />.</summary>
internal sealed record DiscoveryDevice(
    string Topic,
    JsonObject Device,
    IReadOnlyList<DiscoveryComponent> Components,
    IReadOnlyList<DisabledDiscoveryComponent> DisabledComponents);