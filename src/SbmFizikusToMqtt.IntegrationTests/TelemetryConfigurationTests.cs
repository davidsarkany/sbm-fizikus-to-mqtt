using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using SbmFizikusToMqtt.IntegrationTests.TestSupport;

namespace SbmFizikusToMqtt.IntegrationTests;

/// <summary>The service name that Grafana (via OTLP) or the Aspire dashboard sees for the bridge.</summary>
[Trait("Category", "Integration")]
public sealed class TelemetryConfigurationTests(MosquittoBroker broker) : IDisposable
{
    private readonly SbmApiMock _sbm = new();

    public void Dispose()
    {
        _sbm.Dispose();
    }

    [Fact]
    public async Task Start_NoServiceNameConfigured_UsesDefaultNameAndVersion()
    {
        // Arrange
        await using var app = new BridgeApplication(broker, _sbm);

        // Act
        var attributes = ResourceAttributes(app);

        // Assert
        Assert.Equal("sbm-fizikus-to-mqtt", attributes["service.name"]);
        Assert.Equal("3.0.0", attributes["service.version"]);
    }

    [Theory]
    [InlineData("OTEL_SERVICE_NAME", "bridge")]
    [InlineData("OTEL_RESOURCE_ATTRIBUTES", "deployment.environment=prod,service.name=bridge")]
    public async Task Start_ServiceNameConfigured_KeepsConfiguredName(string key, string value)
    {
        // Arrange
        await using var app = new BridgeApplication(broker, _sbm, new Dictionary<string, string?> { [key] = value });

        // Act
        var attributes = ResourceAttributes(app);

        // Assert
        Assert.Equal("bridge", attributes["service.name"]);
    }

    private static Dictionary<string, object> ResourceAttributes(BridgeApplication app)
    {
        app.Start();
        return app.Services.GetRequiredService<TracerProvider>().GetResource().Attributes
            .ToDictionary(x => x.Key, x => x.Value);
    }
}