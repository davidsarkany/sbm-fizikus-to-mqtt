using System.Text.Json;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

internal sealed class ApartmentOutdoorTemperatureSensor(string sbmTopic, string homeAssistantTopic)
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
            DeviceClass = "temperature",
            Name = "apartment_outdoor_temperature",
            StateTopic = $"{sbmTopic}/apartment_info",
            UniqueId = "sbm_fizikus-apartment_info_outdoor_temperature",
            UnitOfMeasurement = "°C",
            ValueTemplate = "{{ value_json.outdoor_temperature }}"
        };

        return new MqttMessage
        {
            Topic = $"{homeAssistantTopic}/sensor/sbm_fizikus-apartment-info/outdoor_temperature/config",
            Payload = JsonSerializer.Serialize(payload)
        };
    }
}
