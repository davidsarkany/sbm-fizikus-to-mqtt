using System.Net;
using System.Net.Sockets;
using MQTTnet;
using MQTTnet.Server;

namespace SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

/// <summary>A real MQTT broker (MQTTnet server) running inside the test process on a free local port.</summary>
internal sealed class InProcessBroker : IAsyncDisposable
{
    private readonly MqttServer _server;

    private InProcessBroker(int port)
    {
        Port = port;
        _server = new MqttServerFactory().CreateMqttServer(new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port)
            .Build());
    }

    public int Port { get; }

    public bool IsStarted => _server.IsStarted;

    public void InterceptSubscriptions(Func<InterceptingSubscriptionEventArgs, Task> handler)
    {
        _server.InterceptingSubscriptionAsync += handler;
    }

    public static InProcessBroker Create()
    {
        return new InProcessBroker(GetFreePort());
    }

    public static async Task<InProcessBroker> StartNewAsync()
    {
        var broker = Create();
        await broker.StartAsync();
        return broker;
    }

    public Task StartAsync()
    {
        return _server.StartAsync();
    }

    public Task StopAsync()
    {
        return _server.StopAsync();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetRetainedAsync()
    {
        var messages = await _server.GetRetainedMessagesAsync();
        return messages.ToDictionary(x => x.Topic, x => x.ConvertPayloadToString());
    }

    /// <summary>Publishes a message as another client would.</summary>
    public async Task PublishAsync(string topic, string payload, bool retain = false)
    {
        using var client = new MqttClientFactory().CreateMqttClient();
        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", Port).Build());
        await client.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithRetainFlag(retain)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());
        await client.DisconnectAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_server.IsStarted)
            await _server.StopAsync();
        _server.Dispose();
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}