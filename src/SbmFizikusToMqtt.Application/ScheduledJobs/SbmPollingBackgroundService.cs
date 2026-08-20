using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Application.Configurations;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.ScheduledJobs;

internal sealed class SbmPollingBackgroundService(
    IApartmentService apartmentService,
    IMqttPublisher publisher,
    IOptions<PollingConfiguration> pollingConfiguration,
    TimeProvider timeProvider,
    ILogger<SbmPollingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(pollingConfiguration.Value.PollingIntervalSeconds);
        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PollOnceAsync(stoppingToken);
        }
    }

    internal async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var apartment = await apartmentService.GetApartmentInfo(cancellationToken);
            await publisher.Publish(apartment, cancellationToken);
            logger.LogInformation("Successfully polled SBM data and published to MQTT");

            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("Apartment data: {@Apartment}", apartment);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Polling operation was cancelled");

            if (cancellationToken.IsCancellationRequested)
                throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error while polling SBM Fizikus API");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during SBM polling");
        }
    }
}