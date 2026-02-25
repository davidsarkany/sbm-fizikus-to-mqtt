using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.BackgroundJobs;

internal sealed class InitialSbmPollingJob(
    IMqttClient mqttClient,
    IApartmentService apartmentService,
    IMqttPublisher publisher,
    ILogger<InitialSbmPollingJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the MQTT client to be connected before publishing
        while (!mqttClient.IsConnected && !stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Waiting for MQTT client to connect before initial polling...");
            await Task.Delay(100, stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested) return;

        try
        {
            logger.LogInformation("Executing initial SBM polling on application startup");
            var apartment = await apartmentService.GetApartmentInfo(stoppingToken);
            await publisher.Publish(apartment, stoppingToken);
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