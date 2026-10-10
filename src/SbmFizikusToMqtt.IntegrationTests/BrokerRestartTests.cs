using System.Net;
using SbmFizikusToMqtt.IntegrationTests.TestSupport;

namespace SbmFizikusToMqtt.IntegrationTests;

/// <summary>Uses its own broker because it restarts it.</summary>
[Trait("Category", "Integration")]
public sealed class BrokerRestartTests : IAsyncLifetime
{
    private const string Online = "{\"state\": \"online\"}";

    private readonly MosquittoBroker _broker = new();
    private readonly SbmApiMock _sbm = new();

    public ValueTask InitializeAsync()
    {
        return _broker.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _sbm.Dispose();
        await _broker.DisposeAsync();
    }

    [Fact]
    public async Task BrokerRestart_WithoutPersistence_ReconnectsAndRestoresStateAndDiscovery()
    {
        // Arrange
        await using var app = new BridgeApplication(_broker, _sbm);
        var client = app.Start();
        var discoveryTopic = $"{app.HomeAssistantTopic}/device/{app.SbmTopic}_thermostat_1763/config";
        await using (var observer = await MqttObserver.ConnectAsync(_broker, $"{app.HomeAssistantTopic}/#"))
            await Eventually.TrueAsync(() => observer.Latest(discoveryTopic) is { Length: > 0 }, "discovery was published");

        // Act
        await _broker.StopAsync();
        await Eventually.TrueAsync(async () =>
                (await client.GetAsync("/health/ready", TestContext.Current.CancellationToken)).StatusCode ==
                HttpStatusCode.ServiceUnavailable,
            "readiness reported the lost broker");
        await _broker.StartAsync();

        // Assert: the restarted broker has no retained messages, so everything must be republished.
        await using var afterRestart = await MqttObserver.ConnectAsync(_broker, $"{app.SbmTopic}/#",
            $"{app.HomeAssistantTopic}/#");
        await Eventually.TrueAsync(() => afterRestart.Latest($"{app.SbmTopic}/bridge/state") == Online,
            "the bridge reconnected and is online again");
        await Eventually.TrueAsync(() => afterRestart.Latest(discoveryTopic) is { Length: > 0 },
            "discovery was republished after reconnecting");
        await Eventually.TrueAsync(async () =>
                (await client.GetAsync("/health/ready", TestContext.Current.CancellationToken)).StatusCode == HttpStatusCode.OK,
            "readiness recovered");
    }
}