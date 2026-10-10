using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

namespace SbmFizikusToMqtt.Infrastructure.Tests.Mqtt;

/// <summary>Behaviour of the MQTT adapter against a real (in-process) broker.</summary>
public sealed class MqttGatewayTests
{
    private const string BridgeStateTopic = "sbm_fizikus/bridge/state";
    private const string Online = "{\"state\": \"online\"}";
    private const string Offline = "{\"state\": \"offline\"}";

    [Fact]
    public async Task StartAsync_BrokerAvailable_PublishesRetainedOnlineAndSignalsConnected()
    {
        // Arrange
        await using var broker = await InProcessBroker.StartNewAsync();
        await using var fixture = new GatewayFixture(broker);

        // Act
        await fixture.Gateway.StartAsync(CancellationToken.None);

        // Assert
        await Eventually.TrueAsync(() => fixture.ConnectedCount == 1, "the gateway signalled the connection");
        Assert.True(fixture.Status.IsMqttConnected);
        var retained = await broker.GetRetainedAsync();
        Assert.Equal(Online, retained[BridgeStateTopic]);
    }

    [Fact]
    public async Task ExecuteAsync_BrokerUnavailableAtStartup_ConnectsOnceItStarts()
    {
        // Arrange
        await using var broker = InProcessBroker.Create();
        await using var fixture = new GatewayFixture(broker);
        await fixture.Gateway.StartAsync(CancellationToken.None);
        await Task.Delay(300, TestContext.Current.CancellationToken);
        Assert.False(fixture.Status.IsMqttConnected);

        // Act
        await broker.StartAsync();

        // Assert
        await Eventually.TrueAsync(() => fixture.ConnectedCount == 1, "the gateway connected after the broker started");
    }

    [Fact]
    public async Task ExecuteAsync_BrokerRestarted_ReconnectsAndSignalsConnectedAgain()
    {
        // Arrange
        await using var broker = await InProcessBroker.StartNewAsync();
        await using var fixture = new GatewayFixture(broker);
        await fixture.Gateway.StartAsync(CancellationToken.None);
        await Eventually.TrueAsync(() => fixture.ConnectedCount == 1, "the gateway connected");

        // Act
        await broker.StopAsync();
        await Eventually.TrueAsync(() => !fixture.Status.IsMqttConnected, "the gateway noticed the broker stopped");
        await broker.StartAsync();

        // Assert
        await Eventually.TrueAsync(() => fixture.ConnectedCount == 2, "the gateway reconnected without a restart");
        Assert.True(fixture.Status.IsMqttConnected);
        await Eventually.TrueAsync(async () => (await broker.GetRetainedAsync()).GetValueOrDefault(BridgeStateTopic)
                                               == Online, "online was published again after reconnecting");
    }

    [Fact]
    public async Task OnMessageReceived_SubscribedTopics_EnqueuesCommands()
    {
        // Arrange
        await using var broker = await InProcessBroker.StartNewAsync();
        await using var fixture = new GatewayFixture(broker);
        await fixture.Gateway.StartAsync(CancellationToken.None);
        await Eventually.TrueAsync(() => fixture.ConnectedCount == 1, "the gateway connected");

        // Act
        await broker.PublishAsync("sbm_fizikus/devices/1763/set", "{\"value\": 21.5}");
        await broker.PublishAsync("homeassistant/status", "online");
        await broker.PublishAsync("sbm_fizikus/devices/1763", "not subscribed");

        // Assert
        await Eventually.TrueAsync(() => fixture.ReceivedMessages().Count == 2, "both subscribed messages arrived");
        Assert.Equal(
        [
            new MqttMessageReceived("sbm_fizikus/devices/1763/set", "{\"value\": 21.5}"),
            new MqttMessageReceived("homeassistant/status", "online")
        ], fixture.ReceivedMessages());
    }

    [Fact]
    public async Task OnMessageReceived_RetainedBeforeConnect_EnqueuesAsRetained()
    {
        // Arrange
        await using var broker = await InProcessBroker.StartNewAsync();
        await broker.PublishAsync("sbm_fizikus/devices/1763/set", "{\"value\": 21.5}", retain: true);
        await using var fixture = new GatewayFixture(broker);

        // Act
        await fixture.Gateway.StartAsync(CancellationToken.None);
        await Eventually.TrueAsync(() => fixture.ReceivedMessages().Count == 1, "the retained message arrived");
        await broker.PublishAsync("sbm_fizikus/devices/1763/set", "{\"value\": 22}");

        // Assert
        await Eventually.TrueAsync(() => fixture.ReceivedMessages().Count == 2, "the live message arrived");
        Assert.Equal(
        [
            new MqttMessageReceived("sbm_fizikus/devices/1763/set", "{\"value\": 21.5}", Retained: true),
            new MqttMessageReceived("sbm_fizikus/devices/1763/set", "{\"value\": 22}")
        ], fixture.ReceivedMessages());
    }

    [Fact]
    public async Task OnMessageReceived_EmptyPayload_EnqueuesEmptyString()
    {
        // Arrange
        await using var broker = await InProcessBroker.StartNewAsync();
        await using var fixture = new GatewayFixture(broker);
        await fixture.Gateway.StartAsync(CancellationToken.None);
        await Eventually.TrueAsync(() => fixture.ConnectedCount == 1, "the gateway connected");

        // Act
        await broker.PublishAsync("sbm_fizikus/devices/1763/set", string.Empty);

        // Assert
        await Eventually.TrueAsync(() => fixture.ReceivedMessages().Count == 1, "the message arrived");
        Assert.Equal(new MqttMessageReceived("sbm_fizikus/devices/1763/set", string.Empty),
            fixture.ReceivedMessages()[0]);
    }

    [Fact]
    public async Task PublishAsync_RetainedMessage_IsStoredByBroker()
    {
        // Arrange
        await using var broker = await InProcessBroker.StartNewAsync();
        await using var fixture = new GatewayFixture(broker);
        await fixture.Gateway.StartAsync(CancellationToken.None);
        await Eventually.TrueAsync(() => fixture.ConnectedCount == 1, "the gateway connected");

        // Act
        await fixture.Gateway.PublishAsync(new MqttMessage
        {
            Topic = "sbm_fizikus/apartment_info",
            Payload = "{\"connected\":true}",
            Retain = true
        }, CancellationToken.None);

        // Assert
        await Eventually.TrueAsync(async () => (await broker.GetRetainedAsync()).ContainsKey("sbm_fizikus/apartment_info"),
            "the broker retained the message");
    }

    [Fact]
    public async Task StopAsync_Connected_PublishesRetainedOfflineAndDisconnects()
    {
        // Arrange
        await using var broker = await InProcessBroker.StartNewAsync();
        var fixture = new GatewayFixture(broker);
        await fixture.Gateway.StartAsync(CancellationToken.None);
        await Eventually.TrueAsync(() => fixture.ConnectedCount == 1, "the gateway connected");

        // Act
        await fixture.DisposeAsync();

        // Assert
        Assert.False(fixture.Status.IsMqttConnected);
        var retained = await broker.GetRetainedAsync();
        Assert.Equal(Offline, retained[BridgeStateTopic]);
    }

    [Fact]
    public async Task PublishAsync_NotConnected_Throws()
    {
        // Arrange
        await using var fixture = new GatewayFixture(InProcessBroker.Create());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Gateway.PublishAsync(
            new MqttMessage { Topic = "t", Payload = "p" }, CancellationToken.None));
    }
}