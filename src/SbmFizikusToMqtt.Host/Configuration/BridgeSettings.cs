using System.ComponentModel.DataAnnotations;

namespace SbmFizikusToMqtt.Host.Configuration;

// The settings have setters, not init accessors: the configuration binding source generator (needed for Native AOT)
// binds into an existing instance and silently skips init-only properties. [Required] replaces 'required'.

/// <summary>Section <c>SbmConnector</c> (environment: <c>SbmConnector__*</c>); key names are unchanged from v2.</summary>
public sealed record SbmConnectorSettings
{
    public const string SectionName = "SbmConnector";

    [Required] public string Username { get; set; } = "";

    [Required] public string Password { get; set; } = "";

    [Required][Url] public string BaseUrl { get; set; } = "";

    [Range(1, int.MaxValue)] public int PollingIntervalSeconds { get; set; } = 120;

    public bool OutdoorWeatherEnabled { get; set; }
}

/// <summary>Section <c>MqttConnector:MqttServer</c> (environment: <c>MqttConnector__MqttServer__*</c>).</summary>
public sealed record MqttServerSettings
{
    public const string SectionName = "MqttConnector:MqttServer";

    [Required] public string Host { get; set; } = "";

    [Range(1, 65535)] public int Port { get; set; } = 1883;

    public string? Username { get; set; }

    public string? Password { get; set; }

    [Required] public string ClientId { get; set; } = "sbm-fizikus-mqtt";
}

/// <summary>Section <c>PublisherConfiguration</c> (environment: <c>PublisherConfiguration__*</c>).</summary>
public sealed record PublisherSettings
{
    public const string SectionName = "PublisherConfiguration";

    [Required] public string SbmTopic { get; set; } = "sbm_fizikus";

    [Required] public string HomeAssistantTopic { get; set; } = "homeassistant";

    public bool ApartmentSystemModeDiscoveryEnabled { get; set; }
    public bool ApartmentOutdoorTemperatureDiscoveryEnabled { get; set; }
    public bool ApartmentOutdoorHumidityDiscoveryEnabled { get; set; }
    public bool ThermostatTemperatureDiscoveryEnabled { get; set; }
    public bool ThermostatTargetTemperatureDiscoveryEnabled { get; set; }
    public bool ThermostatHumidityDiscoveryEnabled { get; set; }
    public bool ThermostatSystemModeDiscoveryEnabled { get; set; }
    public bool ClimateDiscoveryEnabled { get; set; } = true;

    public bool OutdoorDiscoveryEnabled =>
        ApartmentOutdoorTemperatureDiscoveryEnabled || ApartmentOutdoorHumidityDiscoveryEnabled;
}