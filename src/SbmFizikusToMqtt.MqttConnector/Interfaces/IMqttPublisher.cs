using SbmFizikusToMqtt.Domain;

namespace SbmFizikusToMqtt.MqttConnector.Interfaces;

public interface IMqttPublisher
{
    Task Publish(Apartment apartment, CancellationToken cancellationToken = default);
    Task PublishHomeAssistantAutoDiscovery(Apartment apartment, CancellationToken cancellationToken = default);
}