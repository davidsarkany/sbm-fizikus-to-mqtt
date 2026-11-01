using SbmFizikusToMqtt.Core.Ports;

namespace SbmFizikusToMqtt.Core.Tests.TestSupport;

internal sealed class FakeMqttGateway : IMqttGateway
{
    public List<MqttMessage> Published { get; } = [];

    /// <summary>When set, every publish fails with this exception.</summary>
    public Exception? Failure { get; set; }

    public Task PublishAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Failure is not null)
            return Task.FromException(Failure);

        Published.Add(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<MqttMessage> DiscoveryMessages(string homeAssistantTopic = "homeassistant")
    {
        return Published.Where(x => x.Topic.StartsWith($"{homeAssistantTopic}/", StringComparison.Ordinal)).ToList();
    }
}