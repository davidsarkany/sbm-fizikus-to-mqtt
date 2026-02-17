using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Domain;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;

public interface IAutoDiscoveryGenerator
{
    IEnumerable<MqttMessage> Generate(Apartment apartment);
}