using System.Text.Json;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

internal sealed class ApartmentOutdoorHumiditySensor(string sbmTopic, string homeAssistantTopic)
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
            DeviceClass = "humidity",
            Name = "apartment_outdoor_humidity",
            StateTopic = $"{sbmTopic}/apartment_info",
            UniqueId = "sbm_fizikus-apartment_info_outdoor_humidity",
            UnitOfMeasurement = "%",
            ValueTemplate = "{{ value_json.outdoor_humidity }}"
        };

        return new MqttMessage
        {
            Topic = $"{homeAssistantTopic}/sensor/sbm_fizikus-apartment-info/outdoor_humidity/config",
            Payload = JsonSerializer.Serialize(payload)
        };
    }
}
