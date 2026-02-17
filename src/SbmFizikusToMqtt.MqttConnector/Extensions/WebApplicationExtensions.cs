using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using SbmFizikusToMqtt.MqttConnector.Configurations;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.MqttConnector.Services;

namespace SbmFizikusToMqtt.MqttConnector.Extensions;

public static class WebApplicationExtensions
{
    public static IServiceCollection ConfigureMqttPublisher(this IServiceCollection serviceCollection,
        MqttServerConfiguration mqttServerConfiguration,
        MqttConnectorPublisherConfiguration mqttConnectorPublisherConfiguration)
    {
        serviceCollection.AddSingleton(TimeProvider.System);
        serviceCollection.AddSingleton<MqttClientOptions>(_ =>
            new MqttClientOptionsBuilder()
                .WithTcpServer(mqttServerConfiguration.Host, mqttServerConfiguration.Port)
                .WithCredentials(mqttServerConfiguration.Username, mqttServerConfiguration.Password)
                .WithClientId(mqttServerConfiguration.ClientId)
                .WithWillTopic($"{mqttConnectorPublisherConfiguration.SbmTopic}/bridge/state")
                .WithWillPayload("{\"state\": \"offline\"}")
                .Build());

        serviceCollection.AddSingleton<IMqttClient>(serviceProvider =>
        {
            var mqttClient = new MqttClientFactory().CreateMqttClient();

            mqttClient.DisconnectedAsync += e =>
            {
                if (!e.ClientWasConnected) return Task.CompletedTask;

                var logger = serviceProvider.GetService<ILogger<IMqttClient>>();
                logger?.LogError(e.Exception, "MQTT client disconnected unexpectedly. Initiating graceful shutdown.");

                var lifetime = serviceProvider.GetService<IHostApplicationLifetime>();
                lifetime?.StopApplication();

                return Task.CompletedTask;
            };

            return mqttClient;
        });

        serviceCollection.AddHostedService<MqttConnectionService>();
        serviceCollection.AddSingleton<IMqttPublisher, MqttPublisher>();
        return serviceCollection;
    }
}