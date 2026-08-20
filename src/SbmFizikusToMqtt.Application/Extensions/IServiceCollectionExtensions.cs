using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SbmFizikusToMqtt.Application.BackgroundJobs;
using SbmFizikusToMqtt.Application.ScheduledJobs;

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

    public static IServiceCollection AddSbmPollingBackgroundService(this IServiceCollection services, IConfiguration configuration)
    {
        var pollingIntervalSeconds = configuration.GetSection("SbmConnector:PollingIntervalSeconds").Get<int?>();
        if (pollingIntervalSeconds is not > 0)
        {
            throw new InvalidOperationException("Missing or invalid configuration: SbmConnector:PollingIntervalSeconds");
        }

        return services.AddHostedService<SbmPollingBackgroundService>();
    }
}