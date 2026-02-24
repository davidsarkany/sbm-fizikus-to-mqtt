using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MQTTnet;
using SbmFizikusToMqtt.Application.BackgroundJobs;
using SbmFizikusToMqtt.Application.Extensions;
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

    private static void RegisterCommonDependencies(IServiceCollection services)
    {
        services.AddSingleton(new Mock<IMqttClient>().Object);
        services.AddSingleton(new Mock<IApartmentService>().Object);
        services.AddSingleton(new Mock<IMqttPublisher>().Object);
        services.AddSingleton(new Mock<IOptionsMonitor<MqttConnectorPublisherConfiguration>>().Object);
        services.AddSingleton(new Mock<ILogger<MqttListener>>().Object);
        services.AddSingleton(new Mock<ILogger<InitialSbmPollingJob>>().Object);
    }
}

