using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SbmFizikusToMqtt.IntegrationTests.TestSupport;

/// <summary>
///     The real host configured through the same keys as production. Every instance gets its own topic prefixes so
///     tests sharing one broker cannot see each other's retained messages.
/// </summary>
public sealed class BridgeApplication(
    MosquittoBroker broker,
    SbmApiMock sbm,
    IReadOnlyDictionary<string, string?>? overrides = null) : WebApplicationFactory<Program>
{
    public string SbmTopic { get; } = $"sbm_{Guid.NewGuid():N}";

    public string HomeAssistantTopic { get; } = $"ha_{Guid.NewGuid():N}";

    /// <summary>The bridge starts lazily; this starts it and returns an HTTP client for the health endpoints.</summary>
    public HttpClient Start()
    {
        return CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var settings = new Dictionary<string, string?>
        {
            ["SbmConnector:Username"] = "user@example.com",
            ["SbmConnector:Password"] = "secret",
            ["SbmConnector:BaseUrl"] = sbm.Url,
            ["SbmConnector:PollingIntervalSeconds"] = "2",
            ["MqttConnector:MqttServer:Host"] = broker.Host,
            ["MqttConnector:MqttServer:Port"] = broker.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["MqttConnector:MqttServer:ClientId"] = $"bridge-{Guid.NewGuid():N}",
            ["PublisherConfiguration:SbmTopic"] = SbmTopic,
            ["PublisherConfiguration:HomeAssistantTopic"] = HomeAssistantTopic,
            ["PublisherConfiguration:ThermostatTemperatureDiscoveryEnabled"] = "true",
            ["Logging:LogLevel:Default"] = "Warning"
        };

        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
            settings[key] = value;

        foreach (var (key, value) in settings)
            builder.UseSetting(key, value);
    }
}