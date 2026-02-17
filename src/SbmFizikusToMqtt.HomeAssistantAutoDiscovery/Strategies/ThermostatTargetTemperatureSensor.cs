using System.Text.Json;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

internal sealed class ThermostatTargetTemperatureSensor(string sbmTopic, string homeAssistantTopic)
    : IThermostatDiscoveryStrategy
{
    public MqttMessage CreatePayload(Thermostat thermostat)
    {
        var payload = new SensorAutoDiscovery
        {
            Availability =
            [
                new Availability
                {
                    Topic = $"{sbmTopic}/bridge/state",
                    ValueTemplate = "{{ value_json.state }}"
                }
            ],
            DeviceClass = "temperature",
            Name = thermostat.Name == null ? null : thermostat.Name + "_target_temperature",
            StateClass = "measurement",
            StateTopic = $"{sbmTopic}/devices/{thermostat.Id}",
            UniqueId = $"sbm_fizikus-{thermostat.Id}_temperature_target",
            UnitOfMeasurement = "°C",
            ValueTemplate = "{{ value_json.target_temperature }}"
        };

        return new MqttMessage
        {
            Topic = $"{homeAssistantTopic}/sensor/sbm_fizikus-sensors-{thermostat.Id}/target_temperature/config",
            Payload = JsonSerializer.Serialize(payload)
        };
    }
}