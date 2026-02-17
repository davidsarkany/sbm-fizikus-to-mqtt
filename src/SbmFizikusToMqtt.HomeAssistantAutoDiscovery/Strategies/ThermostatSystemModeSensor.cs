using System.Text.Json;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

internal sealed class ThermostatSystemModeSensor(string sbmTopic, string homeAssistantTopic)
    : IThermostatDiscoveryStrategy
{
    public MqttMessage CreatePayload(Thermostat thermostat)
    {
        var payload = new SensorAutoDiscovery
        {
            Availability = new List<Availability>
            {
                new()
                {
                    Topic = $"{sbmTopic}/bridge/state",
                    ValueTemplate = "{{ value_json.state }}"
                }
            },
            EntityCategory = "diagnostic",
            Name = thermostat.Name == null ? null : thermostat.Name + "_system_mode",
            StateTopic = $"{sbmTopic}/devices/{thermostat.Id}",
            UniqueId = $"sbm_fizikus-{thermostat.Id}_system_mode",
            ValueTemplate = "{{ value_json.system_mode }}"
        };

        return new MqttMessage
        {
            Topic = $"{homeAssistantTopic}/sensor/sbm_fizikus-sensors-{thermostat.Id}/system_mode/config",
            Payload = JsonSerializer.Serialize(payload)
        };
    }
}