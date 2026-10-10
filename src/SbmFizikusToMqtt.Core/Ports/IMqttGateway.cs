namespace SbmFizikusToMqtt.Core.Ports;

/// <summary>Outbound MQTT operations needed by the bridge logic.</summary>
public interface IMqttGateway
{
    Task PublishAsync(MqttMessage message, CancellationToken cancellationToken);
}