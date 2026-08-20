using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using SbmFizikusToMqtt.Application.Models;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Configurations;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.MqttConnector.Configurations;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.BackgroundJobs;

/// <summary>
///     Background service that listens for MQTT messages to control thermostat temperatures.
/// </summary>
internal sealed class MqttListener(
    IMqttConnection mqttConnection,
    IMqttClient mqttClient,
    IApartmentService apartmentService,
    IMqttPublisher publisher,
    IOptionsMonitor<MqttConnectorPublisherConfiguration> mqttConnectorPublisherConfiguration,
    IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration> homeAssistantAutoDiscoveryConfiguration,
    ILogger<MqttListener> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly CancellationTokenSource _cts = new();
    private readonly string _homeAssistantTopic = homeAssistantAutoDiscoveryConfiguration.CurrentValue.HomeAssistantTopic;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _cts.Token);
        var cancellationToken = linkedCts.Token;

        // Wait for the MQTT connection to be established before subscribing
        await mqttConnection.WaitUntilConnectedAsync(cancellationToken);

        var subscribeTopic = BuildSubscribeTopic();

        mqttClient.ApplicationMessageReceivedAsync += HandleMessageReceived;

        try
        {
            await SubscribeToTopicAsync(subscribeTopic, cancellationToken);
            await SubscribeToTopicAsync($"{_homeAssistantTopic}/status", cancellationToken);
            await WaitForCancellationAsync(cancellationToken);
        }
        finally
        {
            mqttClient.ApplicationMessageReceivedAsync -= HandleMessageReceived;
        }
    }

    public override void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        base.Dispose();
    }

    private string BuildSubscribeTopic()
    {
        var baseTopic = mqttConnectorPublisherConfiguration.CurrentValue.SbmTopic;
        return $"{baseTopic}/devices/+/set";
    }

    private async Task SubscribeToTopicAsync(string topic, CancellationToken cancellationToken)
    {
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topic)
            .Build();

        await mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Subscribed to {Topic}", topic);
    }

    private static async Task WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
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
            if (topic.Equals($"{_homeAssistantTopic}/status", StringComparison.Ordinal))
            {
                try
                {
                    if (payload.Equals("online", StringComparison.Ordinal))
                    {
                        logger.LogInformation("Home Assistant reported online, re-publishing all discovery messages and current state");
                        var apartment = await apartmentService.GetApartmentInfo(_cts.Token);
                        await publisher.Publish(apartment, _cts.Token);
                        await publisher.PublishHomeAssistantAutoDiscovery(apartment, _cts.Token);
                    }
                }
                catch (Exception ex)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                        logger.LogError(ex, "Error processing Home Assistant status message");
                }

                return;
            }

            var request = DeserializeRequest(payload);
            if (request == null) return;

            await ProcessTemperatureChangeRequestAsync(request);
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

    private MqttChangeTemperatureRequest? DeserializeRequest(string payload)
    {
        try
        {
            var request = JsonSerializer.Deserialize<MqttChangeTemperatureRequest>(payload, JsonSerializerOptions);
            if (request == null)
            {
                if (logger.IsEnabled(LogLevel.Error))
                    logger.LogError("Failed to deserialize payload: {Payload}", payload);
                return null;
            }

            return request;
        }
        catch (JsonException ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(ex, "Invalid JSON format in payload: {Payload}", payload);
            return null;
        }
    }

    private async Task ProcessTemperatureChangeRequestAsync(MqttChangeTemperatureRequest request)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Changing temperature for thermostat {ThermostatId} to {Temperature}",
                request.Id,
                request.Value);
        }

        await apartmentService.ChangeTemperature(request.Id, request.Value, _cts.Token);
        var apartment = await apartmentService.GetApartmentInfo(_cts.Token);
        await publisher.Publish(apartment, _cts.Token);
    }
}