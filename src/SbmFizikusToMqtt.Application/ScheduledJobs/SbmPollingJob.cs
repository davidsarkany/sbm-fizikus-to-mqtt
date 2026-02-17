using Microsoft.Extensions.Logging;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace SbmFizikusToMqtt.Application.ScheduledJobs;

internal sealed class SbmPollingJob(
    IApartmentService apartmentService,
    IMqttPublisher publisher,
    ILogger<SbmPollingJob> logger) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var apartment = await apartmentService.GetApartmentInfo(cancellationToken);
            await publisher.Publish(apartment, cancellationToken);
            logger.LogDebug("Successfully polled SBM data and published to MQTT");

            if (logger.IsEnabled(LogLevel.Trace))
                logger.LogTrace("Apartment data: {@Apartment}", apartment);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Polling operation was cancelled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error while polling SBM Fizikus API");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during SBM polling");
            throw;
        }
    }
}