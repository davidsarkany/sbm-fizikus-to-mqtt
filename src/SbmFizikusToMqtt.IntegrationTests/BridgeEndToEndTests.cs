using System.Net;
using SbmFizikusToMqtt.IntegrationTests.TestSupport;

[assembly: AssemblyFixture(typeof(MosquittoBroker))]

namespace SbmFizikusToMqtt.IntegrationTests;

/// <summary>The whole bridge against a real Mosquitto broker and the fake SBM API, as Home Assistant sees it.</summary>
[Trait("Category", "Integration")]
public sealed class BridgeEndToEndTests(MosquittoBroker broker) : IDisposable
{
    private const string Online = "{\"state\": \"online\"}";
    private const string Offline = "{\"state\": \"offline\"}";

    private readonly SbmApiMock _sbm = new();

    public void Dispose()
    {
        _sbm.Dispose();
    }

    [Fact]
    public async Task Start_FreshBroker_PublishesOnlineStateAndDiscovery()
    {
        // Arrange
        await using var app = new BridgeApplication(broker, _sbm);
        await using var observer = await ObserveAsync(app);

        // Act
        app.Start();

        // Assert
        await Eventually.TrueAsync(() => observer.Latest($"{app.SbmTopic}/bridge/state") == Online,
            "the bridge announced itself online");
        await Eventually.TrueAsync(() => observer.Latest($"{app.SbmTopic}/apartment_info") is not null,
            "the apartment state was published");
        foreach (var id in new[] { 1763, 1764, 1765, 1766 })
        {
            await Eventually.TrueAsync(() => observer.Latest($"{app.SbmTopic}/devices/{id}") is not null,
                $"thermostat {id} state was published");
            await Eventually.TrueAsync(
                () => observer.Latest($"{app.HomeAssistantTopic}/device/{app.SbmTopic}_thermostat_{id}/config") is { Length: > 0 },
                $"thermostat {id} discovery was published");
        }

        Assert.Contains("\"connected\":true", observer.Latest($"{app.SbmTopic}/apartment_info"));
        Assert.Contains("\"last_update\":\"2026-10-10T12:45:11.578277+00:00\"",
            observer.Latest($"{app.SbmTopic}/apartment_info"));
    }

    [Fact]
    public async Task TemperatureCommand_FromHomeAssistant_ReachesSbm()
    {
        // Arrange
        await using var app = new BridgeApplication(broker, _sbm);
        await using var observer = await ObserveAsync(app);
        app.Start();
        await Eventually.TrueAsync(() => observer.Latest($"{app.SbmTopic}/devices/1763") is not null,
            "the bridge is running");

        // Act
        await observer.PublishAsync($"{app.SbmTopic}/devices/1763/set", "{\"id\": 1763, \"value\": 21.5}");

        // Assert
        await Eventually.TrueAsync(() => _sbm.Requests("update_thermostat_settings").Count == 1,
            "the setpoint was sent to SBM");
        var payload = _sbm.Requests("update_thermostat_settings")[0];
        Assert.Equal(1763, payload.GetProperty("thermostat_id").GetInt32());
        Assert.Equal(21.5, payload.GetProperty("temp_setpoint_degC").GetDouble());
    }

    [Fact]
    public async Task TemperatureCommand_OutOfRange_IsNotSentToSbm()
    {
        // Arrange
        await using var app = new BridgeApplication(broker, _sbm);
        await using var observer = await ObserveAsync(app);
        app.Start();
        await Eventually.TrueAsync(() => observer.Latest($"{app.SbmTopic}/devices/1763") is not null,
            "the bridge is running");

        // Act
        await observer.PublishAsync($"{app.SbmTopic}/devices/1763/set", "{\"value\": 40}");
        await observer.PublishAsync($"{app.SbmTopic}/devices/1763/set", "{\"value\": 22}");

        // Assert
        await Eventually.TrueAsync(() => _sbm.Requests("update_thermostat_settings").Count == 1,
            "the valid command was processed after the invalid one");
        Assert.Equal(22, _sbm.Requests("update_thermostat_settings")[0].GetProperty("temp_setpoint_degC").GetDouble());
    }

    [Fact]
    public async Task HealthEndpoints_AfterFirstPoll_ReportHealthy()
    {
        // Arrange
        await using var app = new BridgeApplication(broker, _sbm);
        var client = app.Start();

        // Act & Assert
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", TestContext.Current.CancellationToken)).StatusCode);
        await Eventually.TrueAsync(async () =>
                (await client.GetAsync("/health/ready", TestContext.Current.CancellationToken)).StatusCode == HttpStatusCode.OK,
            "the bridge became ready");
    }

    [Fact]
    public async Task Stop_Graceful_PublishesRetainedOffline()
    {
        // Arrange
        var app = new BridgeApplication(broker, _sbm);
        await using var observer = await ObserveAsync(app);
        app.Start();
        await Eventually.TrueAsync(() => observer.Latest($"{app.SbmTopic}/bridge/state") == Online, "the bridge is online");

        // Act
        await app.DisposeAsync();

        // Assert
        await Eventually.TrueAsync(() => observer.Latest($"{app.SbmTopic}/bridge/state") == Offline,
            "the bridge announced it went offline");
    }

    private Task<MqttObserver> ObserveAsync(BridgeApplication app)
    {
        return MqttObserver.ConnectAsync(broker, $"{app.SbmTopic}/#", $"{app.HomeAssistantTopic}/#");
    }
}