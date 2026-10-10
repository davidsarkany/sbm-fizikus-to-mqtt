using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SbmFizikusToMqtt.Host.Configuration;
using SbmFizikusToMqtt.Host.Health;
using SbmFizikusToMqtt.Host.Logging;

try
{
    // The slim builder leaves out what the bridge does not use (HTTPS, IIS, regex route constraints) and keeps the
    // Native AOT executable small.
    var builder = WebApplication.CreateSlimBuilder(args);

    // Two log providers: the console, and OpenTelemetry from the service defaults, which exports logs, traces and
    // metrics when OTEL_EXPORTER_OTLP_ENDPOINT is set (the Aspire AppHost, or a Grafana Alloy in production).
    builder.Logging.ClearProviders();
    builder.Logging.AddBridgeConsole();
    builder.AddServiceDefaults();
    builder.AddBridgeTelemetry();
    builder.Services.AddBridge(builder.Configuration);

    var app = builder.Build();

    // Liveness: the process is running. Readiness: MQTT connected and SBM polled recently.
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready",
        new HealthCheckOptions { Predicate = check => check.Tags.Contains(BridgeReadinessCheck.ReadyTag) });

    await app.RunAsync();
    return Environment.ExitCode;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // Startup failures (e.g. invalid configuration) end with a non-zero exit code for the restart policy.
    // Disposing the factory flushes the console queue before the process exits.
    using (var loggerFactory = LoggerFactory.Create(logging => logging.AddBridgeConsole()))
        loggerFactory.CreateLogger<Program>().LogCritical(ex, "SBM Fizikus to MQTT terminated unexpectedly");
    return 1;
}

// Exposed for WebApplicationFactory<Program> in the integration tests.
public partial class Program;