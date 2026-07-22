using Microsoft.Extensions.DependencyInjection;
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
        serviceCollection.AddSingleton<MqttClientOptions>(_ =>
            new MqttClientOptionsBuilder()
                .WithTcpServer(mqttServerConfiguration.Host, mqttServerConfiguration.Port)
                .WithCredentials(mqttServerConfiguration.Username, mqttServerConfiguration.Password)
                .WithClientId(mqttServerConfiguration.ClientId)
                .WithWillTopic($"{mqttConnectorPublisherConfiguration.SbmTopic}/bridge/state")
                .WithWillPayload("{\"state\": \"offline\"}")
                .Build());

        serviceCollection.AddSingleton<IMqttClient>(_ => new MqttClientFactory().CreateMqttClient());

        serviceCollection.AddHostedService<MqttConnectionService>();
        serviceCollection.AddSingleton<IMqttPublisher, MqttPublisher>();
        return serviceCollection;
    }
}