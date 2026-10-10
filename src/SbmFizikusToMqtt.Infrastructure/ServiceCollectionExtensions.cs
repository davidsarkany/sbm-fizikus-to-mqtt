using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Infrastructure.Mqtt;
using SbmFizikusToMqtt.Infrastructure.Sbm;

namespace SbmFizikusToMqtt.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the SBM adapter as <see cref="ISbmGateway" />. <see cref="SbmOptions" /> must be configured by
    ///     the host.
    /// </summary>
    public static IServiceCollection AddSbmGateway(this IServiceCollection services)
    {
        services.AddHttpClient(SbmClient.HttpClientName, (serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SbmOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            // Retries transient failures (5xx, 408, 429, network errors) with a 30 s total and 10 s per-attempt timeout.
            .AddStandardResilienceHandler();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<SbmClient>();
        services.AddSingleton<ISbmGateway, SbmSession>();
        return services;
    }

    /// <summary>
    ///     Registers the MQTT adapter as <see cref="IMqttGateway" /> and as the hosted service owning the connection.
    ///     The host must register <see cref="MqttOptions" />, <c>MqttTopics</c>, <c>BridgeStatus</c> and
    ///     <c>BridgeCommandQueue</c>. Register it before the bridge loop so it stops after it and can announce
    ///     <c>offline</c>.
    /// </summary>
    public static IServiceCollection AddMqttGateway(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<MqttGateway>();
        services.AddSingleton<IMqttGateway>(serviceProvider => serviceProvider.GetRequiredService<MqttGateway>());
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<MqttGateway>());
        return services;
    }
}