using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SbmFizikusToMqtt.Application.BackgroundJobs;
using SbmFizikusToMqtt.Application.ScheduledJobs;
using SbmFizikusToMqtt.Application.Services;

namespace SbmFizikusToMqtt.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAutoDiscoveryPublicationState(this IServiceCollection services)
    {
        services.TryAddSingleton<IAutoDiscoveryPublicationState, AutoDiscoveryPublicationState>();
        return services;
    }

    public static IServiceCollection AddMqttListenerBackgroundService(this IServiceCollection services)
    {
        services.AddAutoDiscoveryPublicationState();
        return services.AddHostedService<MqttListener>();
    }

    public static IServiceCollection AddInitialSbmPollingJob(this IServiceCollection services)
    {
        services.AddAutoDiscoveryPublicationState();
        return services.AddHostedService<InitialSbmPollingJob>();
    }

    public static IServiceCollection AddSbmPollingBackgroundService(this IServiceCollection services, IConfiguration configuration)
    {
        var pollingIntervalSeconds = configuration.GetSection("SbmConnector:PollingIntervalSeconds").Get<int?>();
        if (pollingIntervalSeconds is not > 0)
        {
            throw new InvalidOperationException("Missing or invalid configuration: SbmConnector:PollingIntervalSeconds");
        }

        services.AddAutoDiscoveryPublicationState();
        return services.AddHostedService<SbmPollingBackgroundService>();
    }
}