using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Tests.TestSupport;

namespace SbmFizikusToMqtt.Core.Tests.Bridge;

public sealed class BridgeTelemetryTests
{
    private readonly FakeMqttGateway _mqtt = new();
    private readonly FakeSbmGateway _sbm = new();
    private readonly BridgeProcessor _sut;

    public BridgeTelemetryTests()
    {
        _sut = new BridgeProcessor(_sbm, _mqtt, new BridgeStatus(TimeProvider.System), TestApartments.Topics,
            new DiscoveryOptions(), TimeProvider.System, NullLogger<BridgeProcessor>.Instance);
    }

    [Fact]
    public async Task HandleAsync_MqttConnected_TracesCommandWithDiscoveryChild()
    {
        // Arrange & Act
        var spans = await CaptureAsync(new MqttConnected());

        // Assert
        var command = Assert.Single(spans, x => x.DisplayName == "bridge mqtt_connected");
        var discovery = Assert.Single(spans, x => x.DisplayName == "bridge publish_discovery");
        Assert.Equal(command.SpanId, discovery.ParentSpanId);
        Assert.Equal("mqtt_connected", command.GetTagItem("bridge.command"));
        Assert.Equal(3, discovery.GetTagItem("bridge.discovery.devices"));
        Assert.Equal(ActivityStatusCode.Unset, command.Status);
    }

    [Fact]
    public async Task HandleAsync_PollWithUnchangedDiscovery_TracesOnlyThePoll()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);

        // Act
        var spans = await CaptureAsync(new PollRequested());

        // Assert
        var poll = Assert.Single(spans);
        Assert.Equal("bridge poll", poll.DisplayName);
        Assert.Equal("poll", poll.GetTagItem("bridge.command"));
    }

    [Theory]
    [InlineData("homeassistant/status", "online", "bridge homeassistant_status")]
    [InlineData("sbm_fizikus/devices/1763/set", "{\"value\": 21.5}", "bridge set_temperature")]
    [InlineData("sbm_fizikus/other", "x", "bridge mqtt_message")]
    public async Task HandleAsync_MqttMessage_NamesSpanAfterMessageKind(string topic, string payload,
        string expectedName)
    {
        // Arrange & Act
        var spans = await CaptureAsync(new MqttMessageReceived(topic, payload));

        // Assert
        var command = Assert.Single(spans, x => x.Parent?.DisplayName == "test");
        Assert.Equal(expectedName, command.DisplayName);
        Assert.Equal(topic, command.GetTagItem("mqtt.topic"));
    }

    [Fact]
    public async Task HandleAsync_InvalidTemperatureCommand_TagsRejectionWithoutError()
    {
        // Arrange & Act
        var spans = await CaptureAsync(new MqttMessageReceived("sbm_fizikus/devices/1763/set", "{\"value\": 40}"));

        // Assert
        var span = Assert.Single(spans);
        Assert.Equal("temperature must be between 5 and 35 in 0.5 steps", span.GetTagItem("bridge.rejected"));
        Assert.Equal(ActivityStatusCode.Unset, span.Status);
    }

    [Fact]
    public async Task HandleAsync_SbmFailure_MarksCommandSpanAsFailed()
    {
        // Arrange
        _sbm.Failure = new InvalidOperationException("SBM down");

        // Act
        var spans = await CaptureAsync(new PollRequested());

        // Assert
        var span = Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal("SBM down", span.StatusDescription);
        Assert.Contains(span.Events, x => x.Name == "exception");
    }

    private Task<List<Activity>> CaptureAsync(BridgeCommand command)
    {
        return SpanCapture.CaptureAsync(() => _sut.HandleAsync(command, CancellationToken.None),
            BridgeTelemetry.ActivitySourceName);
    }
}