using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Infrastructure.Sbm;

namespace SbmFizikusToMqtt.Host.Configuration;

public static class TelemetryExtensions
{
    /// <summary>Service name used when the environment does not configure one.</summary>
    public const string DefaultServiceName = "sbm-fizikus-to-mqtt";

    /// <summary>
    ///     Adds the bridge's trace sources and a stable service name on top of the service defaults. Telemetry is
    ///     exported only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set (e.g. to a local Grafana Alloy).
    /// </summary>
    public static WebApplicationBuilder AddBridgeTelemetry(this WebApplicationBuilder builder)
    {
        var openTelemetry = builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource(BridgeTelemetry.ActivitySourceName)
                .AddSource(SbmTelemetry.ActivitySourceName));

        // Without a configured name the SDK reports "unknown_service:...". Aspire sets OTEL_SERVICE_NAME per resource
        // and its dashboard relies on it, so the default must never override an explicit name.
        if (!HasConfiguredServiceName(builder.Configuration))
            openTelemetry.ConfigureResource(resource =>
                resource.AddService(DefaultServiceName, serviceVersion: HostVersion.Current));

        return builder;
    }

    private static bool HasConfiguredServiceName(IConfiguration configuration)
    {
        return !string.IsNullOrWhiteSpace(configuration["OTEL_SERVICE_NAME"])
               || (configuration["OTEL_RESOURCE_ATTRIBUTES"] ?? string.Empty)
               .Split(',')
               .Any(x => x.Trim().StartsWith("service.name=", StringComparison.Ordinal));
    }
}