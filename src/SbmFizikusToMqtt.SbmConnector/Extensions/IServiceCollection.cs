using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.SbmConnector.Configurations;
using SbmFizikusToMqtt.SbmConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Services;

namespace SbmFizikusToMqtt.SbmConnector.Extensions;

public static class IServiceCollectionExtension
{
    public static IServiceCollection ConfigureSbmConnector(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient("SbmClient", (serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IOptions<SbmConfiguration>>().Value;
            client.BaseAddress = new Uri(config.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        serviceCollection.AddSingleton(TimeProvider.System);
        serviceCollection.AddSingleton<ITokenService, TokenService>();
        serviceCollection.AddSingleton<ISbmService, SbmService>();
        serviceCollection.AddSingleton<IApartmentService, ApartmentService>();
        return serviceCollection;
    }
}