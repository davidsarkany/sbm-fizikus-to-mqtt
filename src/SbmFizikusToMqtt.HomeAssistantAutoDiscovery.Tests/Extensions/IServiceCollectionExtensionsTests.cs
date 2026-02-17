using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Configurations;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Extensions;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_RegistersAutoDiscoveryGenerator()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureServicesWithOptions(services);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IAutoDiscoveryGenerator>();
        Assert.NotNull(service);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_RegistersThermostatHumiditySensor()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureServicesWithOptions(services);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<ThermostatHumiditySensor>();
        Assert.NotNull(service);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_RegistersThermostatSystemModeSensor()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureServicesWithOptions(services);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<ThermostatSystemModeSensor>();
        Assert.NotNull(service);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_RegistersThermostatTemperatureSensor()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureServicesWithOptions(services);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<ThermostatTemperatureSensor>();
        Assert.NotNull(service);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_RegistersThermostatTargetTemperatureSensor()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureServicesWithOptions(services);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<ThermostatTargetTemperatureSensor>();
        Assert.NotNull(service);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_RegistersApartmentSystemModeSensor()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureServicesWithOptions(services);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<ApartmentSystemModeSensor>();
        Assert.NotNull(service);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_RegistersThermostatClimate()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureServicesWithOptions(services);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<ThermostatClimate>();
        Assert.NotNull(service);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_ThermostatHumiditySensor_UsesCorrectTopics()
    {
        // Arrange
        var services = new ServiceCollection();
        var sbmTopic = "sbm/test";
        var homeAssistantTopic = "homeassistant/test";
        ConfigureServicesWithOptions(services, sbmTopic, homeAssistantTopic);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();
        var sensor = serviceProvider.GetRequiredService<ThermostatHumiditySensor>();

        // Assert
        Assert.NotNull(sensor);
        // We can verify the sensor is created by generating a payload and checking the topics
        var testThermostat = new Thermostat
        {
            Id = 1,
            Name = "Test",
            Temperature = 20.0,
            Humidity = 50.0,
            TargetTemperature = 22.0,
            DewPoint = 10.0,
            Active = true,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var message = sensor.CreatePayload(testThermostat);
        Assert.Contains(homeAssistantTopic, message.Topic, StringComparison.Ordinal);
        Assert.Contains(sbmTopic, message.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_ThermostatSystemModeSensor_UsesCorrectTopics()
    {
        // Arrange
        var services = new ServiceCollection();
        var sbmTopic = "sbm/custom";
        var homeAssistantTopic = "ha/custom";
        ConfigureServicesWithOptions(services, sbmTopic, homeAssistantTopic);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();
        var sensor = serviceProvider.GetRequiredService<ThermostatSystemModeSensor>();

        // Assert
        Assert.NotNull(sensor);
        var testThermostat = new Thermostat
        {
            Id = 2,
            Name = "Test2",
            Temperature = 21.0,
            Humidity = 55.0,
            TargetTemperature = 23.0,
            DewPoint = 11.0,
            Active = true,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var message = sensor.CreatePayload(testThermostat);
        Assert.Contains(homeAssistantTopic, message.Topic, StringComparison.Ordinal);
        Assert.Contains(sbmTopic, message.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_ThermostatTemperatureSensor_UsesCorrectTopics()
    {
        // Arrange
        var services = new ServiceCollection();
        var sbmTopic = "sbm/production";
        var homeAssistantTopic = "ha/production";
        ConfigureServicesWithOptions(services, sbmTopic, homeAssistantTopic);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();
        var sensor = serviceProvider.GetRequiredService<ThermostatTemperatureSensor>();

        // Assert
        Assert.NotNull(sensor);
        var testThermostat = new Thermostat
        {
            Id = 3,
            Name = "Test3",
            Temperature = 19.0,
            Humidity = 45.0,
            TargetTemperature = 21.0,
            DewPoint = 9.0,
            Active = true,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var message = sensor.CreatePayload(testThermostat);
        Assert.Contains(homeAssistantTopic, message.Topic, StringComparison.Ordinal);
        Assert.Contains(sbmTopic, message.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_ThermostatTargetTemperatureSensor_UsesCorrectTopics()
    {
        // Arrange
        var services = new ServiceCollection();
        var sbmTopic = "sbm/target";
        var homeAssistantTopic = "ha/target";
        ConfigureServicesWithOptions(services, sbmTopic, homeAssistantTopic);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();
        var sensor = serviceProvider.GetRequiredService<ThermostatTargetTemperatureSensor>();

        // Assert
        Assert.NotNull(sensor);
        var testThermostat = new Thermostat
        {
            Id = 4,
            Name = "Test4",
            Temperature = 22.0,
            Humidity = 60.0,
            TargetTemperature = 24.0,
            DewPoint = 12.0,
            Active = true,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var message = sensor.CreatePayload(testThermostat);
        Assert.Contains(homeAssistantTopic, message.Topic, StringComparison.Ordinal);
        Assert.Contains(sbmTopic, message.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_ApartmentSystemModeSensor_UsesCorrectTopics()
    {
        // Arrange
        var services = new ServiceCollection();
        var sbmTopic = "sbm/apartment";
        var homeAssistantTopic = "ha/apartment";
        ConfigureServicesWithOptions(services, sbmTopic, homeAssistantTopic);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();
        var sensor = serviceProvider.GetRequiredService<ApartmentSystemModeSensor>();

        // Assert
        Assert.NotNull(sensor);
        var testApartment = new Apartment
        {
            SystemMode = "heating",
            Thermostats = Array.Empty<Thermostat>(),
            LastUpdate = DateTimeOffset.UtcNow,
            RelayConnectionActive = true,
            ThermostatsConnectionActive = true
        };
        var message = sensor.CreatePayload(testApartment);
        Assert.Contains(homeAssistantTopic, message.Topic, StringComparison.Ordinal);
        Assert.Contains(sbmTopic, message.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_ThermostatClimate_UsesCorrectTopics()
    {
        // Arrange
        var services = new ServiceCollection();
        var sbmTopic = "sbm/climate";
        var homeAssistantTopic = "ha/climate";
        ConfigureServicesWithOptions(services, sbmTopic, homeAssistantTopic);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();
        var climate = serviceProvider.GetRequiredService<ThermostatClimate>();

        // Assert
        Assert.NotNull(climate);
        var testThermostat = new Thermostat
        {
            Id = 5,
            Name = "Test5",
            Temperature = 23.0,
            Humidity = 65.0,
            TargetTemperature = 25.0,
            DewPoint = 13.0,
            Active = true,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var message = climate.CreatePayload(testThermostat);
        Assert.Contains(homeAssistantTopic, message.Topic, StringComparison.Ordinal);
        Assert.Contains(sbmTopic, message.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_AllServices_RegisteredAsSingletons()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureServicesWithOptions(services);

        // Act
        services.ConfigureHomeAssistantAutoDiscovery();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var generator1 = serviceProvider.GetRequiredService<IAutoDiscoveryGenerator>();
        var generator2 = serviceProvider.GetRequiredService<IAutoDiscoveryGenerator>();
        Assert.Same(generator1, generator2);

        var humidity1 = serviceProvider.GetRequiredService<ThermostatHumiditySensor>();
        var humidity2 = serviceProvider.GetRequiredService<ThermostatHumiditySensor>();
        Assert.Same(humidity1, humidity2);

        var systemMode1 = serviceProvider.GetRequiredService<ThermostatSystemModeSensor>();
        var systemMode2 = serviceProvider.GetRequiredService<ThermostatSystemModeSensor>();
        Assert.Same(systemMode1, systemMode2);

        var temperature1 = serviceProvider.GetRequiredService<ThermostatTemperatureSensor>();
        var temperature2 = serviceProvider.GetRequiredService<ThermostatTemperatureSensor>();
        Assert.Same(temperature1, temperature2);

        var targetTemp1 = serviceProvider.GetRequiredService<ThermostatTargetTemperatureSensor>();
        var targetTemp2 = serviceProvider.GetRequiredService<ThermostatTargetTemperatureSensor>();
        Assert.Same(targetTemp1, targetTemp2);

        var apartmentMode1 = serviceProvider.GetRequiredService<ApartmentSystemModeSensor>();
        var apartmentMode2 = serviceProvider.GetRequiredService<ApartmentSystemModeSensor>();
        Assert.Same(apartmentMode1, apartmentMode2);

        var climate1 = serviceProvider.GetRequiredService<ThermostatClimate>();
        var climate2 = serviceProvider.GetRequiredService<ThermostatClimate>();
        Assert.Same(climate1, climate2);
    }

    [Fact]
    public void ConfigureHomeAssistantAutoDiscovery_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        ConfigureServicesWithOptions(services);

        // Act
        var result = services.ConfigureHomeAssistantAutoDiscovery();

        // Assert
        Assert.Same(services, result);
    }

    private static void ConfigureServicesWithOptions(
        IServiceCollection services,
        string sbmTopic = "sbm/test",
        string homeAssistantTopic = "homeassistant/test")
    {
        var configuration = new HomeAssistantAutoDiscoveryConfiguration
        {
            SbmTopic = sbmTopic,
            HomeAssistantTopic = homeAssistantTopic,
            ThermostatTemperatureDiscoveryEnabled = true,
            ThermostatTargetTemperatureDiscoveryEnabled = true,
            ThermostatHumidityDiscoveryEnabled = true,
            ThermostatSystemModeDiscoveryEnabled = true,
            ClimateDiscoveryEnabled = true,
            ApartmentSystemModeDiscoveryEnabled = true
        };

        services.AddSingleton<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>(_ =>
            new TestOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>(configuration));
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; }

        public T Get(string? name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            return null!;
        }
    }
}