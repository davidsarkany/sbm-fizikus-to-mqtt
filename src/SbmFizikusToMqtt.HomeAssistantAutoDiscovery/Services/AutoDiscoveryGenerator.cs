using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Configurations;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Services;

internal sealed class AutoDiscoveryGenerator : IAutoDiscoveryGenerator
{
    private readonly List<IApartmentDiscoveryStrategy> _apartmentDiscovery = [];
    private readonly List<IThermostatDiscoveryStrategy> _thermostatDiscovery = [];

    public AutoDiscoveryGenerator(
        IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration> optionsMonitor,
        ApartmentSystemModeSensor apartmentSystemModeSensor,
        ThermostatHumiditySensor thermostatHumiditySensor,
        ThermostatTemperatureSensor thermostatTemperatureSensor,
        ThermostatTargetTemperatureSensor thermostatTargetTemperatureSensor,
        ThermostatSystemModeSensor thermostatSystemModeSensor,
        ThermostatClimate thermostatClimate
    )
    {
        var options = optionsMonitor.CurrentValue;

        if (options.ApartmentSystemModeDiscoveryEnabled)
            _apartmentDiscovery.Add(apartmentSystemModeSensor);

        if (options.ThermostatHumidityDiscoveryEnabled)
            _thermostatDiscovery.Add(thermostatHumiditySensor);

        if (options.ThermostatTemperatureDiscoveryEnabled)
            _thermostatDiscovery.Add(thermostatTemperatureSensor);

        if (options.ThermostatTargetTemperatureDiscoveryEnabled)
            _thermostatDiscovery.Add(thermostatTargetTemperatureSensor);

        if (options.ThermostatSystemModeDiscoveryEnabled)
            _thermostatDiscovery.Add(thermostatSystemModeSensor);

        if (options.ClimateDiscoveryEnabled)
            _thermostatDiscovery.Add(thermostatClimate);
    }

    public IEnumerable<MqttMessage> Generate(Apartment apartment)
    {
        // Generate apartment discovery messages
        foreach (var strategy in _apartmentDiscovery) yield return strategy.CreatePayload(apartment);

        // Generate thermostat discovery messages
        foreach (var thermostat in apartment.Thermostats)
        {
            foreach (var strategy in _thermostatDiscovery)
            {
                yield return strategy.CreatePayload(thermostat);
            }
        }
    }
}