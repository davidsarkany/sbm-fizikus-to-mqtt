using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Host.Configuration;

namespace SbmFizikusToMqtt.Host.Health;

/// <summary>Ready while MQTT is connected and SBM was polled successfully within three polling intervals.</summary>
internal sealed class BridgeReadinessCheck(
    BridgeStatus status,
    IOptions<SbmConnectorSettings> settings,
    TimeProvider timeProvider) : IHealthCheck
{
    public const string ReadyTag = "ready";
    private const int MaxMissedPolls = 3;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!status.IsMqttConnected)
            return Task.FromResult(HealthCheckResult.Unhealthy("MQTT broker is not connected"));

        var lastPoll = status.LastSuccessfulPoll;
        if (lastPoll is null)
            return Task.FromResult(HealthCheckResult.Unhealthy("SBM has not been polled successfully yet"));

        var maxAge = TimeSpan.FromSeconds(settings.Value.PollingIntervalSeconds * MaxMissedPolls);
        var age = timeProvider.GetUtcNow() - lastPoll.Value;
        return Task.FromResult(age <= maxAge
            ? HealthCheckResult.Healthy($"Last successful SBM poll {age.TotalSeconds:0} s ago")
            : HealthCheckResult.Unhealthy($"Last successful SBM poll {age.TotalSeconds:0} s ago (limit {maxAge.TotalSeconds:0} s)"));
    }
}