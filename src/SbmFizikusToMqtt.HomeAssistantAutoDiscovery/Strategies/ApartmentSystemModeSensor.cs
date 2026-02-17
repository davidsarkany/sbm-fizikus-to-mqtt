using System.Text.Json;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

internal sealed class ApartmentSystemModeSensor(string sbmTopic, string homeAssistantTopic)
    : IApartmentDiscoveryStrategy
{
    public MqttMessage CreatePayload(Apartment apartment)
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
            Name = "apartment_system_mode",
            StateTopic = $"{sbmTopic}/apartment_info",
            UniqueId = "sbm_fizikus-apartment_info_system_mode",
            ValueTemplate = "{{ value_json.system_mode }}"
        };

        return new MqttMessage
        {
            Topic = $"{homeAssistantTopic}/sensor/sbm_fizikus-apartment-info/system_mode/config",
            Payload = JsonSerializer.Serialize(payload)
        };
    }
}