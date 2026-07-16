using System.ComponentModel.DataAnnotations;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Configurations;

public sealed record HomeAssistantAutoDiscoveryConfiguration
{
    [Required] public required string SbmTopic { get; init; }

    [Required] public required string HomeAssistantTopic { get; init; }

    [Required] public required bool ThermostatTemperatureDiscoveryEnabled { get; init; }

    [Required] public required bool ThermostatTargetTemperatureDiscoveryEnabled { get; init; }

    [Required] public required bool ThermostatHumidityDiscoveryEnabled { get; init; }

    [Required] public required bool ThermostatSystemModeDiscoveryEnabled { get; init; }

    [Required] public required bool ClimateDiscoveryEnabled { get; init; }

    [Required] public required bool ApartmentSystemModeDiscoveryEnabled { get; init; }

    [Required] public required bool ApartmentOutdoorTemperatureDiscoveryEnabled { get; init; }

    [Required] public required bool ApartmentOutdoorHumidityDiscoveryEnabled { get; init; }
}