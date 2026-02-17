using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Configurations;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Services;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureHomeAssistantAutoDiscovery(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ThermostatHumiditySensor>(x =>
        {
            var options = x.GetService<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>()!.CurrentValue;
            return new ThermostatHumiditySensor(options.SbmTopic, options.HomeAssistantTopic);
        });
        serviceCollection.AddSingleton<ThermostatSystemModeSensor>(x =>
        {
            var options = x.GetService<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>()!.CurrentValue;
            return new ThermostatSystemModeSensor(options.SbmTopic, options.HomeAssistantTopic);
        });
        serviceCollection.AddSingleton<ThermostatTemperatureSensor>(x =>
        {
            var options = x.GetService<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>()!.CurrentValue;
            return new ThermostatTemperatureSensor(options.SbmTopic, options.HomeAssistantTopic);
        });
        serviceCollection.AddSingleton<ThermostatTargetTemperatureSensor>(x =>
        {
            var options = x.GetService<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>()!.CurrentValue;
            return new ThermostatTargetTemperatureSensor(options.SbmTopic, options.HomeAssistantTopic);
        });
        serviceCollection.AddSingleton<ApartmentSystemModeSensor>(x =>
        {
            var options = x.GetService<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>()!.CurrentValue;
            return new ApartmentSystemModeSensor(options.SbmTopic, options.HomeAssistantTopic);
        });
        serviceCollection.AddSingleton<ThermostatClimate>(x =>
        {
            var options = x.GetService<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>()!.CurrentValue;
            return new ThermostatClimate(options.SbmTopic, options.HomeAssistantTopic);
        });
        serviceCollection.AddSingleton<IAutoDiscoveryGenerator, AutoDiscoveryGenerator>();
        return serviceCollection;
    }
}