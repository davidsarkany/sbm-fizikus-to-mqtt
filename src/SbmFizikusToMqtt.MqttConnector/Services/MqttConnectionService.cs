using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace SbmFizikusToMqtt.MqttConnector.Services;

internal sealed class MqttConnectionService(
    IMqttClient mqttClient,
    MqttClientOptions mqttClientOptions,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<MqttConnectionService> logger) : IHostedService
{
    private bool _isGracefulDisconnect;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _isGracefulDisconnect = false;

        mqttClient.DisconnectedAsync += HandleDisconnectedAsync;

        logger.LogInformation("Connecting to MQTT broker...");
        await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);
        logger.LogInformation("Successfully connected to MQTT broker");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isGracefulDisconnect = true;

        if (mqttClient.IsConnected)
        {
            logger.LogInformation("Disconnecting from MQTT broker...");
            await mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken);
            logger.LogInformation("Disconnected from MQTT broker");
        }

        mqttClient.DisconnectedAsync -= HandleDisconnectedAsync;
    }

    internal Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        if (_isGracefulDisconnect || !e.ClientWasConnected)
            return Task.CompletedTask;

        logger.LogError(e.Exception,
            "MQTT client disconnected unexpectedly (Reason: {Reason}). Initiating application shutdown to allow container restart.",
            e.Reason);

        hostApplicationLifetime.StopApplication();
        return Task.CompletedTask;
    }
}