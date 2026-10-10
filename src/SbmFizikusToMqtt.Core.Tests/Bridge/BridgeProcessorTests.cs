using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Core.Tests.TestSupport;

namespace SbmFizikusToMqtt.Core.Tests.Bridge;

public sealed class BridgeProcessorTests
{
    private const string SetTopic = "sbm_fizikus/devices/1763/set";

    private readonly RecordingLogger<BridgeProcessor> _logger = new();
    private readonly FakeMqttGateway _mqtt = new();
    private readonly FakeSbmGateway _sbm = new();
    private readonly BridgeStatus _status;
    private readonly BridgeProcessor _sut;
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 10, 10, 13, 0, 0, TimeSpan.Zero));

    public BridgeProcessorTests()
    {
        _status = new BridgeStatus(_timeProvider);
        _sut = new BridgeProcessor(_sbm, _mqtt, _status, TestApartments.Topics,
            new DiscoveryOptions { ThermostatTemperatureEnabled = true, SoftwareVersion = "3.0.0-test" },
            _timeProvider, _logger);
    }

    [Fact]
    public async Task HandleAsync_MqttConnected_PublishesStateThenDiscovery()
    {
        // Arrange
        _sbm.Apartment = TestApartments.Create(thermostats: TestApartments.LivingRoom());

        // Act
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);

        // Assert
        Snapshot.Match(MqttMessageFormatter.Format(_mqtt.Published));
    }

    [Fact]
    public async Task HandleAsync_PollWithUnchangedThermostats_PublishesStateButNotDiscovery()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _mqtt.Published.Clear();

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        Assert.Empty(_mqtt.DiscoveryMessages());
        Assert.Equal(["sbm_fizikus/apartment_info", "sbm_fizikus/devices/1763", "sbm_fizikus/devices/1764"],
            _mqtt.Published.Select(x => x.Topic));
        Assert.All(_mqtt.Published, x => Assert.True(x.Retain));
    }

    [Fact]
    public async Task HandleAsync_PollAfterSbmOutageOnConnect_PublishesDiscovery()
    {
        // Arrange
        _sbm.Failure = new HttpRequestException("SBM unavailable");
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _sbm.Failure = null;

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        Assert.Contains(_mqtt.Published, x => x.Topic == "homeassistant/device/sbm_fizikus_thermostat_1763/config");
    }

    [Fact]
    public async Task HandleAsync_PollAfterThermostatRenamed_RepublishesDiscovery()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _mqtt.Published.Clear();
        _sbm.Apartment = TestApartments.Create(thermostats: TestApartments.LivingRoom(name: "nappali"));

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        var discovery = Assert.Single(_mqtt.DiscoveryMessages(),
            x => x.Topic == "homeassistant/device/sbm_fizikus_thermostat_1763/config");
        Assert.Contains("\"name\":\"nappali\"", discovery.Payload);
    }

    [Fact]
    public async Task HandleAsync_HomeAssistantOnline_RepublishesStateAndDiscovery()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _mqtt.Published.Clear();

        // Act
        await _sut.HandleAsync(new MqttMessageReceived("homeassistant/status", "online"), CancellationToken.None);

        // Assert
        Assert.Contains(_mqtt.Published, x => x.Topic == "sbm_fizikus/apartment_info");
        Assert.Contains(_mqtt.Published, x => x.Topic == "homeassistant/device/sbm_fizikus_apartment/config");
    }

    [Fact]
    public async Task HandleAsync_HomeAssistantOffline_DoesNothing()
    {
        // Arrange & Act
        await _sut.HandleAsync(new MqttMessageReceived("homeassistant/status", "offline"), CancellationToken.None);

        // Assert
        Assert.Empty(_mqtt.Published);
        Assert.Equal(0, _sbm.GetApartmentCalls);
    }

    [Fact]
    public async Task HandleAsync_ValidTemperatureCommand_ChangesSetpointAndPublishesState()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _mqtt.Published.Clear();

        // Act
        await _sut.HandleAsync(new MqttMessageReceived(SetTopic, "{\"id\": 1763, \"value\": 21.5}"),
            CancellationToken.None);

        // Assert
        Assert.Equal([(1763, 21.5)], _sbm.TemperatureChanges);
        Assert.Contains(_mqtt.Published, x => x.Topic == "sbm_fizikus/devices/1763");
    }

    [Fact]
    public async Task HandleAsync_TemperatureCommandBeforeFirstPoll_LoadsThermostatsFirst()
    {
        // Arrange & Act
        await _sut.HandleAsync(new MqttMessageReceived(SetTopic, "{\"value\": 22}"), CancellationToken.None);

        // Assert
        Assert.Equal([(1763, 22.0)], _sbm.TemperatureChanges);
    }

    [Theory]
    [InlineData("sbm_fizikus/devices/9999/set", "{\"value\": 21.5}")]
    [InlineData(SetTopic, "{\"id\": 1764, \"value\": 21.5}")]
    [InlineData(SetTopic, "{\"value\": 50}")]
    [InlineData(SetTopic, "garbage")]
    public async Task HandleAsync_InvalidTemperatureCommand_DoesNotCallSbm(string topic, string payload)
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);

        // Act
        await _sut.HandleAsync(new MqttMessageReceived(topic, payload), CancellationToken.None);

        // Assert
        Assert.Empty(_sbm.TemperatureChanges);
    }

    [Fact]
    public async Task HandleAsync_RetainedTemperatureCommand_DoesNotCallSbm()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _mqtt.Published.Clear();

        // Act
        await _sut.HandleAsync(new MqttMessageReceived(SetTopic, "{\"value\": 21.5}", Retained: true),
            CancellationToken.None);

        // Assert
        Assert.Empty(_sbm.TemperatureChanges);
        Assert.Empty(_mqtt.Published);
    }

    [Fact]
    public async Task HandleAsync_RetainedHomeAssistantOnline_RepublishesDiscovery()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _mqtt.Published.Clear();

        // Act
        await _sut.HandleAsync(new MqttMessageReceived("homeassistant/status", "online", Retained: true),
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(_mqtt.DiscoveryMessages());
    }

    [Fact]
    public async Task HandleAsync_PollAfterThermostatRemoved_ClearsItsDiscoveryAndState()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _mqtt.Published.Clear();
        _sbm.Apartment = TestApartments.Create(thermostats: TestApartments.LivingRoom());

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        var cleared = ClearedThermostatMessages();
        Assert.Equal(["homeassistant/device/sbm_fizikus_thermostat_1764/config", "sbm_fizikus/devices/1764"],
            cleared.Select(x => x.Topic));
        Assert.All(cleared, x => Assert.True(x.Retain));
    }

    [Fact]
    public async Task HandleAsync_PollReturnsNoThermostats_KeepsExistingThermostats()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _mqtt.Published.Clear();
        _sbm.Apartment = TestApartments.Create() with { Thermostats = [] };

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        Assert.Empty(ClearedThermostatMessages());
    }

    [Fact]
    public async Task HandleAsync_PollAfterEmptyThermostatList_ClearsThermostatsMissingSinceBefore()
    {
        // Arrange
        await _sut.HandleAsync(new MqttConnected(), CancellationToken.None);
        _sbm.Apartment = TestApartments.Create() with { Thermostats = [] };
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);
        _mqtt.Published.Clear();
        _sbm.Apartment = TestApartments.Create(thermostats: TestApartments.LivingRoom());

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        Assert.Equal(["homeassistant/device/sbm_fizikus_thermostat_1764/config", "sbm_fizikus/devices/1764"],
            ClearedThermostatMessages().Select(x => x.Topic));
    }

    [Fact]
    public async Task HandleAsync_SuccessfulPoll_LogsRefreshSummary()
    {
        // Arrange
        _sbm.Apartment = TestApartments.Create(thermostats: [TestApartments.LivingRoom(), TestApartments.Unnamed()]);
        _timeProvider.Advance(TimeSpan.FromMilliseconds(800));

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        var entry = Assert.Single(_logger.Entries, x => x.Message.StartsWith("Refreshed state", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, entry.Level);
        // The fake clock is 13:00:00.8 and SBM's data is from 12:45:11: 889.8 s old, truncated to whole seconds.
        Assert.Equal("Refreshed state (poll): 2 thermostats, 1 calling, heating, SBM connected, data age 889 s",
            entry.Message);
        Assert.Equal(889L, entry.Values["DataAgeSeconds"]);
        Assert.False(entry.Values.ContainsKey("DurationMs"));
    }

    [Theory]
    [InlineData("mqtt_connected")]
    [InlineData("homeassistant_status")]
    [InlineData("set_temperature")]
    public async Task HandleAsync_RefreshTriggeredByCommand_LogsTrigger(string trigger)
    {
        // Arrange
        BridgeCommand command = trigger switch
        {
            "mqtt_connected" => new MqttConnected(),
            "homeassistant_status" => new MqttMessageReceived("homeassistant/status", "online"),
            _ => new MqttMessageReceived(SetTopic, "{\"value\": 21.5}")
        };

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Contains(_logger.Entries, x => Equals(x.Values.GetValueOrDefault("Trigger"), trigger));
    }

    [Fact]
    public async Task HandleAsync_DisconnectedApartment_LogsSbmDisconnected()
    {
        // Arrange
        _sbm.Apartment = TestApartments.Create(connected: false, systemMode: SystemMode.Cooling);

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        var entry = Assert.Single(_logger.Entries, x => x.Values.ContainsKey("SbmConnection"));
        Assert.Equal(("disconnected", "cooling"), (entry.Values["SbmConnection"], entry.Values["SystemMode"]));
    }

    [Fact]
    public async Task HandleAsync_FailedPoll_DoesNotLogRefresh()
    {
        // Arrange
        _sbm.Failure = new HttpRequestException("SBM unavailable");

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        Assert.DoesNotContain(_logger.Entries, x => x.Values.ContainsKey("DataAgeSeconds"));
        Assert.Contains(_logger.Entries, x => x.Level == LogLevel.Error);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulPoll_MarksPollSucceeded()
    {
        // Arrange & Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        Assert.Equal(_timeProvider.GetUtcNow(), _status.LastSuccessfulPoll);
    }

    [Fact]
    public async Task HandleAsync_SbmFailure_DoesNotThrowOrMarkPoll()
    {
        // Arrange
        _sbm.Failure = new InvalidOperationException("boom");

        // Act
        await _sut.HandleAsync(new PollRequested(), CancellationToken.None);

        // Assert
        Assert.Null(_status.LastSuccessfulPoll);
        Assert.Empty(_mqtt.Published);
    }

    [Fact]
    public async Task HandleAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.HandleAsync(new PollRequested(), cts.Token));
    }

    /// <summary>Empty retained messages, except the apartment config, which is empty when it has no sensors.</summary>
    private List<MqttMessage> ClearedThermostatMessages()
    {
        return _mqtt.Published
            .Where(x => x.Payload.Length == 0 && x.Topic != TestApartments.Topics.ApartmentDiscovery)
            .ToList();
    }
}