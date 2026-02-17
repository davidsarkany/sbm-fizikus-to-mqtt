using Microsoft.Extensions.Logging;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;
using TickerQ.Utilities.Base;

namespace SbmFizikusToMqtt.Application.ScheduledJobs;

internal sealed class SbmPollingJob(
    IApartmentService apartmentService,
    IMqttPublisher publisher,
    ILogger<SbmPollingJob> logger)
{
    [TickerFunction("Polling SBM")]
    public async Task PollSbmData(TickerFunctionContext<string> tickerContext, CancellationToken cancellationToken)
    {
        try
        {
            var apartment = await apartmentService.GetApartmentInfo(cancellationToken);
            await publisher.Publish(apartment, cancellationToken);
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