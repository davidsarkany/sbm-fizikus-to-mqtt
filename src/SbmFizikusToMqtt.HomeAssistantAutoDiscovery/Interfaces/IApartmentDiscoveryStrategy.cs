using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;

internal interface IApartmentDiscoveryStrategy
{
    public MqttMessage CreatePayload(Apartment apartment);
}