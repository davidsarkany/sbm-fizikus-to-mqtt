using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using SbmFizikusToMqtt.Application.Models;
using SbmFizikusToMqtt.MqttConnector.Configurations;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.BackgroundJobs;

internal sealed class MqttListener(
    IMqttClient mqttClient,
    IApartmentService apartmentService,
    IMqttPublisher publisher,
    IOptionsMonitor<MqttConnectorPublisherConfiguration> mqttConnectorPublisherConfiguration,
    ILogger<MqttListener> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the MQTT client to be connected before subscribing
        while (!mqttClient.IsConnected && !stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Waiting for MQTT client to connect...");
            await Task.Delay(100, stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested) return;

        var baseTopic = mqttConnectorPublisherConfiguration.CurrentValue.SbmTopic;
        var subscribeTopic = $"{baseTopic}/devices/+/set";

        mqttClient.ApplicationMessageReceivedAsync += HandleMessageReceived;

        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(subscribeTopic)
            .Build();

        await mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);
        
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Subscribed to {Topic}", subscribeTopic);

        // Keep the service alive until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            mqttClient.ApplicationMessageReceivedAsync -= HandleMessageReceived;
        }
    }

    private async Task HandleMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = args.ApplicationMessage.ConvertPayloadToString();

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Received message on {Topic}: {Payload}", topic, payload);

        try
        {
            var request = JsonSerializer.Deserialize<MqttChangeTemperatureRequest>(payload);
            if (request == null)
            {
                if (logger.IsEnabled(LogLevel.Error))
                    logger.LogError("Failed to deserialize payload: {Payload}", payload);
                return;
            }

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Changing temperature for thermostat {ThermostatId} to {Temperature}", request.Id, request.Value);
            
            await apartmentService.ChangeTemperature(request.Id, request.Value);
            var apartment = await apartmentService.GetApartmentInfo();
            await publisher.Publish(apartment);
        }
        catch (JsonException ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(ex, "Failed to parse JSON payload: {Payload}", payload);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(ex, "Error processing message for topic {Topic}", topic);
        }
    }
}