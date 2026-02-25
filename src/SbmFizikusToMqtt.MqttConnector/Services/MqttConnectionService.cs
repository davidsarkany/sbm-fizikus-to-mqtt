using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace SbmFizikusToMqtt.MqttConnector.Services;

internal sealed class MqttConnectionService(
    IMqttClient mqttClient,
    MqttClientOptions mqttClientOptions,
    ILogger<MqttConnectionService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Connecting to MQTT broker...");
        await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);
        logger.LogInformation("Successfully connected to MQTT broker");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (mqttClient.IsConnected)
        {
            logger.LogInformation("Disconnecting from MQTT broker...");
            await mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken);
            logger.LogInformation("Disconnected from MQTT broker");
        }
    }
}