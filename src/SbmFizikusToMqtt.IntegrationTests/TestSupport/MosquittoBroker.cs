using System.Net;
using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace SbmFizikusToMqtt.IntegrationTests.TestSupport;

/// <summary>
///     An Eclipse Mosquitto broker in Docker. The host port is fixed for the container's lifetime so the bridge can
///     reconnect to the same address after <see cref="RestartAsync" />.
/// </summary>
public sealed class MosquittoBroker : IAsyncLifetime
{
    private const int MqttPort = 1883;

    private readonly IContainer _container;

    public MosquittoBroker()
    {
        Port = GetFreePort();
        _container = new ContainerBuilder("eclipse-mosquitto:2.0.22")
            .WithPortBinding(Port, MqttPort)
            .WithCommand("mosquitto", "-c", "/mosquitto-no-auth.conf")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(MqttPort))
            .Build();
    }

    public string Host => "127.0.0.1";

    public int Port { get; }

    public ValueTask InitializeAsync()
    {
        return new ValueTask(_container.StartAsync());
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public Task StopAsync()
    {
        return _container.StopAsync();
    }

    public Task StartAsync()
    {
        return _container.StartAsync();
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}