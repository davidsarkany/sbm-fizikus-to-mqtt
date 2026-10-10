using System.Collections.Concurrent;
using MQTTnet;

namespace SbmFizikusToMqtt.IntegrationTests.TestSupport;

/// <summary>
///     A test client that sees what Home Assistant would see: it records the latest message per topic (including
///     retained ones and removals) and can publish like Home Assistant does.
/// </summary>
public sealed class MqttObserver : IAsyncDisposable
{
    private readonly IMqttClient _client = new MqttClientFactory().CreateMqttClient();
    private readonly ConcurrentDictionary<string, string> _latest = new(StringComparer.Ordinal);

    private MqttObserver()
    {
        _client.ApplicationMessageReceivedAsync += args =>
        {
            _latest[args.ApplicationMessage.Topic] = args.ApplicationMessage.ConvertPayloadToString() ?? string.Empty;
            return Task.CompletedTask;
        };
    }

    public static async Task<MqttObserver> ConnectAsync(MosquittoBroker broker, params string[] topicFilters)
    {
        var observer = new MqttObserver();
        await observer._client.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer(broker.Host, broker.Port)
            .WithClientId($"observer-{Guid.NewGuid():N}")
            .Build());

        var subscribe = new MqttClientSubscribeOptionsBuilder();
        foreach (var filter in topicFilters)
            subscribe.WithTopicFilter(filter);
        await observer._client.SubscribeAsync(subscribe.Build());
        return observer;
    }

    /// <summary>Latest payload per topic; an empty string means the retained message was removed.</summary>
    public string? Latest(string topic)
    {
        return _latest.GetValueOrDefault(topic);
    }

    public IReadOnlyCollection<string> Topics => _latest.Keys.ToList();

    public Task PublishAsync(string topic, string payload, bool retain = false)
    {
        return _client.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
            await _client.DisconnectAsync();
        _client.Dispose();
    }
}