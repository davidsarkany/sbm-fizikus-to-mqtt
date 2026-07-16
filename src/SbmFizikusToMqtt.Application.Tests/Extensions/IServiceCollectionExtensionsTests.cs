using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MQTTnet;
using SbmFizikusToMqtt.Application.BackgroundJobs;
using SbmFizikusToMqtt.Application.Extensions;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Configurations;
using SbmFizikusToMqtt.MqttConnector.Configurations;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.Tests.Extensions;

public sealed class IServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMqttListenerBackgroundService_RegistersMqttListenerAsHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        RegisterCommonDependencies(services);

        // Act
        services.AddMqttListenerBackgroundService();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is MqttListener);
    }

    [Fact]
    public void AddMqttListenerBackgroundService_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddMqttListenerBackgroundService();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddInitialSbmPollingJob_RegistersInitialSbmPollingJobAsHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        RegisterCommonDependencies(services);

        // Act
        services.AddInitialSbmPollingJob();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is InitialSbmPollingJob);
    }

    [Fact]
    public void AddInitialSbmPollingJob_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddInitialSbmPollingJob();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddBothServices_RegistersBothAsHostedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        RegisterCommonDependencies(services);

        // Act
        services.AddMqttListenerBackgroundService();
        services.AddInitialSbmPollingJob();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
        Assert.Contains(hostedServices, s => s is MqttListener);
        Assert.Contains(hostedServices, s => s is InitialSbmPollingJob);
    }

    [Fact]
    public void AddSbmPollingAsync_WithValidCronExpression_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SbmConnector:PollingCronExpression", "*/5 * * * *" }
            })
            .Build();

        // Act
        var result = services.AddSbmPollingAsync(configuration);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddSbmPollingAsync_MissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddSbmPollingAsync(configuration));
        Assert.Contains("SbmConnector:PollingCronExpression", ex.Message);
    }

    [Fact]
    public void AddSbmPollingAsync_NullCronExpression_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "SbmConnector:PollingCronExpression", null! }
            })
            .Build();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddSbmPollingAsync(configuration));
        Assert.Contains("SbmConnector:PollingCronExpression", ex.Message);
    }

    private static void RegisterCommonDependencies(IServiceCollection services)
    {
        var mqttConnectorPublisherConfigurationMock = new Mock<IOptionsMonitor<MqttConnectorPublisherConfiguration>>();
        mqttConnectorPublisherConfigurationMock.Setup(x => x.CurrentValue).Returns(new MqttConnectorPublisherConfiguration { SbmTopic = "sbm" });

        var homeAssistantAutoDiscoveryConfigurationMock = new Mock<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>();
        homeAssistantAutoDiscoveryConfigurationMock.Setup(x => x.CurrentValue).Returns(new HomeAssistantAutoDiscoveryConfiguration
        {
            SbmTopic = "sbm",
            HomeAssistantTopic = "homeassistant",
            ThermostatTemperatureDiscoveryEnabled = true,
            ThermostatTargetTemperatureDiscoveryEnabled = true,
            ThermostatHumidityDiscoveryEnabled = true,
            ThermostatSystemModeDiscoveryEnabled = true,
            ClimateDiscoveryEnabled = true,
            ApartmentSystemModeDiscoveryEnabled = true,
            ApartmentOutdoorTemperatureDiscoveryEnabled = false,
            ApartmentOutdoorHumidityDiscoveryEnabled = false
        });

        services.AddSingleton(new Mock<IMqttClient>().Object);
        services.AddSingleton(new Mock<IApartmentService>().Object);
        services.AddSingleton(new Mock<IMqttPublisher>().Object);
        services.AddSingleton(mqttConnectorPublisherConfigurationMock.Object);
        services.AddSingleton(homeAssistantAutoDiscoveryConfigurationMock.Object);
        services.AddSingleton(new Mock<ILogger<MqttListener>>().Object);
        services.AddSingleton(new Mock<ILogger<InitialSbmPollingJob>>().Object);
    }
}