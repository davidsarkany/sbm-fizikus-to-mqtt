using SbmFizikusToMqtt.Application.Configurations;
using SbmFizikusToMqtt.Application.Extensions;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Configurations;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Extensions;
using SbmFizikusToMqtt.MqttConnector.Configurations;
using SbmFizikusToMqtt.MqttConnector.Extensions;
using SbmFizikusToMqtt.SbmConnector.Configurations;
using SbmFizikusToMqtt.SbmConnector.Extensions;
using TickerQ.DependencyInjection;

namespace SbmFizikusToMqtt.Web.Extensions;

public static class ServiceCollectionExtensions
{
    private const string SbmCredentialConfigurationSection = "SbmConnector";
    private const string MqttServerConfigurationSection = "MqttConnector:MqttServer";
    private const string PublisherConfigurationSection = "PublisherConfiguration";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplicationConfigurations(IConfiguration configuration)
        {
            services.AddOptions<SbmConfiguration>()
                .Bind(configuration.GetSection(SbmCredentialConfigurationSection))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<PollingConfiguration>()
                .Bind(configuration.GetSection(SbmCredentialConfigurationSection))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<MqttServerConfiguration>()
                .Bind(configuration.GetSection(MqttServerConfigurationSection))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<MqttConnectorPublisherConfiguration>()
                .Bind(configuration.GetSection(PublisherConfigurationSection))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<HomeAssistantAutoDiscoveryConfiguration>()
                .Bind(configuration.GetSection(PublisherConfigurationSection))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }

        public IServiceCollection AddApplicationServices(IConfiguration configuration)
        {
            var mqttServerConfig = configuration
                                       .GetSection(MqttServerConfigurationSection)
                                       .Get<MqttServerConfiguration>()
                                   ?? throw new InvalidOperationException(
                                       $"Missing configuration section: {MqttServerConfigurationSection}");

            var publisherConfig = configuration
                                      .GetSection(PublisherConfigurationSection)
                                      .Get<MqttConnectorPublisherConfiguration>()
                                  ?? throw new InvalidOperationException(
                                      $"Missing configuration section: {PublisherConfigurationSection}");

            services.ConfigureSbmConnector();
            services.ConfigureHomeAssistantAutoDiscovery();
            services.ConfigureMqttPublisher(mqttServerConfig, publisherConfig);
            services.AddMqttListenerBackgroundService();
            services.AddInitialSbmPollingJob();

            return services;
        }

        public IServiceCollection AddTickerQServices()
        {
            services.AddTickerQ(options => { options.ConfigureScheduler(scheduler => scheduler.MaxConcurrency = 2); });

            return services;
        }
    }
}