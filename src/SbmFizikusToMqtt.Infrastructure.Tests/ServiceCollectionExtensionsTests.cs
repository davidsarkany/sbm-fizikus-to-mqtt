using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Infrastructure.Sbm;

namespace SbmFizikusToMqtt.Infrastructure.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSbmGateway_ConfiguredOptions_ResolvesGatewayAndConfiguresHttpClient()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(Options.Create(new SbmOptions
        {
            Username = "user",
            Password = "secret",
            BaseUrl = "https://sbm.example.com"
        }));

        // Act
        services.AddSbmGateway();
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.IsType<SbmSession>(provider.GetRequiredService<ISbmGateway>());
        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(SbmClient.HttpClientName);
        Assert.Equal(new Uri("https://sbm.example.com"), httpClient.BaseAddress);
        Assert.Contains(httpClient.DefaultRequestHeaders.Accept, x => x.MediaType == "application/json");
    }
}