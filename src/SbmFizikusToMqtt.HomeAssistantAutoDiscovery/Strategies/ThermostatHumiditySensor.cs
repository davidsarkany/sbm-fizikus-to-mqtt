using System.Text.Json;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

internal sealed class ThermostatHumiditySensor(string sbmTopic, string homeAssistantTopic)
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
            DeviceClass = "humidity",
            Name = thermostat.Name == null ? null : thermostat.Name + "_humidity",
            StateClass = "measurement",
            StateTopic = $"{sbmTopic}/devices/{thermostat.Id}",
            UniqueId = $"sbm_fizikus-{thermostat.Id}_humidity",
            UnitOfMeasurement = "%",
            ValueTemplate = "{{ value_json.humidity }}"
        };

        return new MqttMessage
        {
            Topic = $"{homeAssistantTopic}/sensor/sbm_fizikus-sensors-{thermostat.Id}/humidity/config",
            Payload = JsonSerializer.Serialize(payload)
        };
    }
}