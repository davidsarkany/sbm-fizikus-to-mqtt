using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Host.Configuration;

namespace SbmFizikusToMqtt.Host;

/// <summary>
///     Runs the bridge: schedules polls and feeds every queued command to <see cref="BridgeProcessor" />, one at a
///     time. This is the only place with timers and the command loop; the logic itself lives in Core.
/// </summary>
internal sealed class BridgeHostedService(
    BridgeProcessor processor,
    BridgeCommandQueue queue,
    BridgeStatus status,
    IOptions<SbmConnectorSettings> settings,
    TimeProvider timeProvider,
    ILogger<BridgeHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var loopCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var commands = ProcessCommandsAsync(loopCancellation.Token);
        var polls = SchedulePollsAsync(loopCancellation.Token);
        try
        {
            // Both loops run until shutdown, so the first one to end decides: WhenAll would hide a failed command
            // loop behind the still-running poll loop.
            await Task.WhenAny(commands, polls);
            await loopCancellation.CancelAsync();
            try
            {
                // Observe both loops before the host can stop their dependencies.
                await Task.WhenAll(commands, polls);
            }
            catch (OperationCanceledException) when (!commands.IsFaulted && !polls.IsFaulted)
            {
                // Cancellation of the sibling loop is expected.
            }

            if (!stoppingToken.IsCancellationRequested)
                throw new InvalidOperationException("A bridge loop ended unexpectedly");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            // BridgeProcessor never throws, so this is a bug; stop with a failure exit code for the restart policy.
            logger.LogCritical(ex, "The bridge loop failed");
            Environment.ExitCode = 1;
            throw;
        }
    }

    private async Task ProcessCommandsAsync(CancellationToken cancellationToken)
    {
        await foreach (var command in queue.ReadAllAsync(cancellationToken))
            await processor.HandleAsync(command, cancellationToken);
    }

    private async Task SchedulePollsAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(settings.Value.PollingIntervalSeconds);
        using var timer = new PeriodicTimer(interval, timeProvider);

        // Every (re)connect already triggers a refresh; polling while disconnected could not publish anything.
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (status.IsMqttConnected)
                queue.Enqueue(new PollRequested());
        }
    }
}