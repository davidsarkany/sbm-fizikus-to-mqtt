using SbmFizikusToMqtt.Domain;

namespace SbmFizikusToMqtt.MqttConnector.Interfaces;

public interface IMqttPublisher
{
    public Task Publish(Apartment apartment, CancellationToken cancellationToken = default);
}