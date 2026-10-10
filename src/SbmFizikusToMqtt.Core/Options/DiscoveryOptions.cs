namespace SbmFizikusToMqtt.Core.Options;

/// <summary>Which Home Assistant entities are published through MQTT discovery.</summary>
public sealed record DiscoveryOptions
{
    public bool ApartmentSystemModeEnabled { get; init; }
    public bool ApartmentOutdoorTemperatureEnabled { get; init; }
    public bool ApartmentOutdoorHumidityEnabled { get; init; }
    public bool ThermostatTemperatureEnabled { get; init; }
    public bool ThermostatTargetTemperatureEnabled { get; init; }
    public bool ThermostatHumidityEnabled { get; init; }
    public bool ThermostatSystemModeEnabled { get; init; }
    public bool ClimateEnabled { get; init; } = true;

    /// <summary>Version reported in the discovery <c>origin</c> block.</summary>
    public string SoftwareVersion { get; init; } = "unknown";
}