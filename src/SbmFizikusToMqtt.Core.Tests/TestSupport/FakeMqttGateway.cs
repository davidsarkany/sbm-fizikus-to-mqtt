using SbmFizikusToMqtt.Core.Ports;

namespace SbmFizikusToMqtt.Core.Tests.TestSupport;

internal sealed class FakeMqttGateway : IMqttGateway
{
    public List<MqttMessage> Published { get; } = [];

    public Task PublishAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Published.Add(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<MqttMessage> DiscoveryMessages(string homeAssistantTopic = "homeassistant")
    {
        return Published.Where(x => x.Topic.StartsWith($"{homeAssistantTopic}/", StringComparison.Ordinal)).ToList();
    }
}