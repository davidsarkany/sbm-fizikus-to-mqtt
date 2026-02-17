using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using SbmFizikusToMqtt.MqttConnector.Configurations;

namespace SbmFizikusToMqtt.MqttConnector.Services;

internal sealed class MqttConnectionService(
    IMqttClient mqttClient,
    MqttClientOptions mqttClientOptions,
    MqttReconnectConfiguration reconnectConfiguration,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<MqttConnectionService> logger) : IHostedService
{
    private bool _isGracefulDisconnect;
    private CancellationTokenSource? _stoppingCts;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isGracefulDisconnect = false;

        mqttClient.DisconnectedAsync += HandleDisconnectedAsync;

        logger.LogInformation("Connecting to MQTT broker...");
        await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);
        logger.LogInformation("Successfully connected to MQTT broker");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isGracefulDisconnect = true;
        await _stoppingCts?.CancelAsync()!;

        if (mqttClient.IsConnected)
        {
            logger.LogInformation("Disconnecting from MQTT broker...");
            await mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken);
            logger.LogInformation("Disconnected from MQTT broker");
        }

        mqttClient.DisconnectedAsync -= HandleDisconnectedAsync;
        _stoppingCts?.Dispose();
    }

    internal async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs e)
    {
        if (_isGracefulDisconnect || !e.ClientWasConnected)
            return;

        logger.LogWarning(e.Exception, "MQTT client disconnected unexpectedly. Attempting to reconnect...");

        var attempt = 0;
        var cancellationToken = _stoppingCts?.Token ?? CancellationToken.None;

        while (attempt < reconnectConfiguration.MaxReconnectAttempts && !cancellationToken.IsCancellationRequested)
        {
            attempt++;
            var delaySeconds = CalculateDelay(attempt);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Reconnect attempt {Attempt}/{MaxAttempts} in {Delay} seconds...",
                    attempt,
                    reconnectConfiguration.MaxReconnectAttempts,
                    delaySeconds);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);
                if (logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("Successfully reconnected to MQTT broker on attempt {Attempt}", attempt);
                return;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Reconnect cancelled");
                return;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                    logger.LogWarning(ex, "Reconnect attempt {Attempt}/{MaxAttempts} failed",
                        attempt, reconnectConfiguration.MaxReconnectAttempts);
            }
        }

        if (logger.IsEnabled(LogLevel.Error))
            logger.LogError(
                "Failed to reconnect to MQTT broker after {MaxAttempts} attempts. Initiating graceful shutdown.",
                reconnectConfiguration.MaxReconnectAttempts);

        hostApplicationLifetime.StopApplication();
    }

    internal int CalculateDelay(int attempt)
    {
        var delay = reconnectConfiguration.InitialDelaySeconds * (int)Math.Pow(2, attempt - 1);
        return Math.Min(delay, reconnectConfiguration.MaxDelaySeconds);
    }
}