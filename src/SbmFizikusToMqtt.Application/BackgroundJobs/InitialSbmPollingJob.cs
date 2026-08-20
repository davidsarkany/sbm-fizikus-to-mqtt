using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.BackgroundJobs;

internal sealed class InitialSbmPollingJob(
    IMqttConnection mqttConnection,
    IApartmentService apartmentService,
    IMqttPublisher publisher,
    ILogger<InitialSbmPollingJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the MQTT connection to be established before publishing
        await mqttConnection.WaitUntilConnectedAsync(stoppingToken);

        try
        {
            logger.LogInformation("Executing initial SBM polling on application startup");
            var apartment = await apartmentService.GetApartmentInfo(stoppingToken);
            await publisher.Publish(apartment, stoppingToken);
            await publisher.PublishHomeAssistantAutoDiscovery(apartment, stoppingToken);
            logger.LogInformation("Initial SBM polling completed successfully");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Initial SBM polling was cancelled");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error during initial SBM polling");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during initial SBM polling");
        }
    }
}