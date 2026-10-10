using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Publishing;
using SbmFizikusToMqtt.Host.Health;
using SbmFizikusToMqtt.Infrastructure;
using SbmFizikusToMqtt.Infrastructure.Mqtt;
using SbmFizikusToMqtt.Infrastructure.Sbm;

namespace SbmFizikusToMqtt.Host.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>Composes the bridge: validated settings, the Core logic, both adapters and the bridge loop.</summary>
    public static IServiceCollection AddBridge(this IServiceCollection services, IConfiguration configuration)
    {
        // Bound and validated by source generators (no reflection), one call per type as the generators require.
        services.AddOptions<SbmConnectorSettings>()
            .Bind(configuration.GetSection(SbmConnectorSettings.SectionName))
            .ValidateOnStart();
        services.AddOptions<MqttServerSettings>()
            .Bind(configuration.GetSection(MqttServerSettings.SectionName))
            .ValidateOnStart();
        services.AddOptions<PublisherSettings>()
            .Bind(configuration.GetSection(PublisherSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SbmConnectorSettings>, SbmConnectorSettingsValidator>();
        services.AddSingleton<IValidateOptions<MqttServerSettings>, MqttServerSettingsValidator>();
        services.AddSingleton<IValidateOptions<PublisherSettings>, PublisherSettingsValidator>();

        services.AddSingleton(serviceProvider => Options.Create(CreateSbmOptions(serviceProvider)));
        services.AddSingleton(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<MqttServerSettings>>().Value;
            return Options.Create(new MqttOptions
            {
                Host = settings.Host,
                Port = settings.Port,
                Username = settings.Username,
                Password = settings.Password,
                ClientId = settings.ClientId
            });
        });
        services.AddSingleton(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<PublisherSettings>>().Value;
            return new MqttTopics(new TopicOptions
            {
                SbmTopic = settings.SbmTopic,
                HomeAssistantTopic = settings.HomeAssistantTopic
            });
        });
        services.AddSingleton(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<PublisherSettings>>().Value;
            return new DiscoveryOptions
            {
                ApartmentSystemModeEnabled = settings.ApartmentSystemModeDiscoveryEnabled,
                ApartmentOutdoorTemperatureEnabled = settings.ApartmentOutdoorTemperatureDiscoveryEnabled,
                ApartmentOutdoorHumidityEnabled = settings.ApartmentOutdoorHumidityDiscoveryEnabled,
                ThermostatTemperatureEnabled = settings.ThermostatTemperatureDiscoveryEnabled,
                ThermostatTargetTemperatureEnabled = settings.ThermostatTargetTemperatureDiscoveryEnabled,
                ThermostatHumidityEnabled = settings.ThermostatHumidityDiscoveryEnabled,
                ThermostatSystemModeEnabled = settings.ThermostatSystemModeDiscoveryEnabled,
                ClimateEnabled = settings.ClimateDiscoveryEnabled,
                SoftwareVersion = HostVersion.Current
            };
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<BridgeStatus>();
        services.AddSingleton<BridgeCommandQueue>();
        services.AddSingleton<BridgeProcessor>();
        services.AddSbmGateway();

        // Registered before the bridge loop: hosted services stop in reverse order, so the gateway is still
        // connected while the loop drains and can then announce 'offline'.
        services.AddMqttGateway();
        services.AddHostedService<BridgeHostedService>();

        services.AddHealthChecks()
            .AddCheck<BridgeReadinessCheck>("bridge", tags: [BridgeReadinessCheck.ReadyTag]);

        return services;
    }

    private static SbmOptions CreateSbmOptions(IServiceProvider serviceProvider)
    {
        var sbm = serviceProvider.GetRequiredService<IOptions<SbmConnectorSettings>>().Value;
        var publisher = serviceProvider.GetRequiredService<IOptions<PublisherSettings>>().Value;

        // Outdoor sensors without outdoor data would never get a value, so their discovery implies the fetch.
        if (publisher.OutdoorDiscoveryEnabled && !sbm.OutdoorWeatherEnabled)
        {
            serviceProvider.GetRequiredService<ILogger<SbmOptions>>().LogWarning(
                "Outdoor sensor discovery is enabled but {Setting} is false; fetching outdoor weather anyway",
                $"{SbmConnectorSettings.SectionName}:{nameof(SbmConnectorSettings.OutdoorWeatherEnabled)}");
        }

        return new SbmOptions
        {
            Username = sbm.Username,
            Password = sbm.Password,
            BaseUrl = sbm.BaseUrl,
            OutdoorWeatherEnabled = sbm.OutdoorWeatherEnabled || publisher.OutdoorDiscoveryEnabled
        };
    }
}