using Microsoft.Extensions.DependencyInjection;
using SbmFizikusToMqtt.Application.BackgroundJobs;

namespace SbmFizikusToMqtt.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMqttListenerBackgroundService(this IServiceCollection services)
    {
        return services.AddHostedService<MqttListener>();
    }

    public static IServiceCollection AddInitialSbmPollingJob(this IServiceCollection services)
    {
        return services.AddHostedService<InitialSbmPollingJob>();
    }
}