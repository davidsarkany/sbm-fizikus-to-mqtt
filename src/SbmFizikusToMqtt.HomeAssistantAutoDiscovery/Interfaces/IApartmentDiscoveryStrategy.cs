using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;

internal interface IApartmentDiscoveryStrategy
{
    MqttMessage CreatePayload(Apartment apartment);
}