using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SbmFizikusToMqtt.Application.BackgroundJobs;
using SbmFizikusToMqtt.Application.ScheduledJobs;
using TickerQ.DependencyInjection;

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

    public static IServiceCollection AddSbmPollingAsync(this IServiceCollection services, IConfiguration configuration)
    {

        var pollingCronExpression = configuration.GetSection("SbmConnector:PollingCronExpression").Get<string>();
        if (pollingCronExpression == null)
        {
            throw new InvalidOperationException("Missing configuration: SbmConnector:PollingCronExpression");
        }

        services.MapTicker<SbmPollingJob>().WithCron(pollingCronExpression);
        return services;
    }
}