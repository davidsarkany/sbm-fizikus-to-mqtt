using System.Text.Json;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

internal sealed class ThermostatClimate(string sbmTopic, string homeAssistantTopic) : IThermostatDiscoveryStrategy
{
    public MqttMessage CreatePayload(Thermostat thermostat)
    {
        var payload = new ClimateAutoDiscovery
        {
            ActionTemplate = "{{ value_json.system_mode }}",
            ActionTopic = $"{sbmTopic}/devices/{thermostat.Id}",
            Availability =
            [
                new Availability
                {
                    Topic = $"{sbmTopic}/bridge/state",
                    ValueTemplate = "{{ value_json.state }}"
                }
            ],
            CurrentHumidityTemplate = "{{ value_json.humidity }}",
            CurrentHumidityTopic = $"{sbmTopic}/devices/{thermostat.Id}",
            CurrentTemperatureTemplate = "{{ value_json.temperature }}",
            CurrentTemperatureTopic = $"{sbmTopic}/devices/{thermostat.Id}",
            TemperatureStateTemplate = "{{ value_json.target_temperature }}",
            TemperatureStateTopic = $"{sbmTopic}/devices/{thermostat.Id}",
            TemperatureUnit = "C",
            UniqueId = $"sbm_fizikus-{thermostat.Id}_climate",
            Modes = ["heat", "cool"],
            ModeStateTemplate =
                "{{ value_json.system_mode | replace('cooling', 'cool') | replace('heating', 'heat') }}",
            ModeStateTopic = $"{sbmTopic}/apartment_info",
            Name = thermostat.Name == null ? null : thermostat.Name + "_climate",
            TemperatureCommandTemplate = "{{ {'id': " + thermostat.Id + ", 'value': value } | to_json }}",
            TemperatureCommandTopic = $"{sbmTopic}/devices/{thermostat.Id}/set"
        };

        return new MqttMessage
        {
            Topic = $"{homeAssistantTopic}/climate/sbm_fizikus-climate-{thermostat.Id}/config",
            Payload = JsonSerializer.Serialize(payload)
        };
    }
}