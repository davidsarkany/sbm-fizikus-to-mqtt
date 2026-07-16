using System.Text.Json;
using Bogus;
using Microsoft.Extensions.Options;
using Moq;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Configurations;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Models;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Services;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Strategies;

namespace SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Tests.Services;

public class AutoDiscoveryGeneratorTests
{
    private const string SbmTopic = "sbm/test";
    private const string HomeAssistantTopic = "homeassistant/test";
    private readonly Faker<Apartment> _apartmentFaker;
    private readonly ApartmentSystemModeSensor _apartmentSystemModeSensor;
    private readonly ApartmentOutdoorTemperatureSensor _apartmentOutdoorTemperatureSensor;
    private readonly ApartmentOutdoorHumiditySensor _apartmentOutdoorHumiditySensor;

    private readonly Mock<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>> _optionsMonitorMock;
    private readonly ThermostatClimate _thermostatClimate;
    private readonly Faker<Thermostat> _thermostatFaker;
    private readonly ThermostatHumiditySensor _thermostatHumiditySensor;
    private readonly ThermostatSystemModeSensor _thermostatSystemModeSensor;
    private readonly ThermostatTargetTemperatureSensor _thermostatTargetTemperatureSensor;
    private readonly ThermostatTemperatureSensor _thermostatTemperatureSensor;

    public AutoDiscoveryGeneratorTests()
    {
        _optionsMonitorMock = new Mock<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>();

        // Create concrete strategy instances
        _apartmentSystemModeSensor = new ApartmentSystemModeSensor(SbmTopic, HomeAssistantTopic);
        _apartmentOutdoorTemperatureSensor = new ApartmentOutdoorTemperatureSensor(SbmTopic, HomeAssistantTopic);
        _apartmentOutdoorHumiditySensor = new ApartmentOutdoorHumiditySensor(SbmTopic, HomeAssistantTopic);
        _thermostatHumiditySensor = new ThermostatHumiditySensor(SbmTopic, HomeAssistantTopic);
        _thermostatTemperatureSensor = new ThermostatTemperatureSensor(SbmTopic, HomeAssistantTopic);
        _thermostatTargetTemperatureSensor = new ThermostatTargetTemperatureSensor(SbmTopic, HomeAssistantTopic);
        _thermostatSystemModeSensor = new ThermostatSystemModeSensor(SbmTopic, HomeAssistantTopic);
        _thermostatClimate = new ThermostatClimate(SbmTopic, HomeAssistantTopic);

        _thermostatFaker = new Faker<Thermostat>()
            .RuleFor(x => x.Id, f => f.Random.Int(1, 100))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.Temperature, f => f.Random.Double(18, 26))
            .RuleFor(x => x.Humidity, f => f.Random.Double(30, 70))
            .RuleFor(x => x.TargetTemperature, f => f.Random.Double(20, 24))
            .RuleFor(x => x.DewPoint, f => f.Random.Double(10, 15))
            .RuleFor(x => x.Active, f => f.Random.Bool())
            .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset());

        _apartmentFaker = new Faker<Apartment>()
            .RuleFor(x => x.SystemMode, f => f.PickRandom("heating", "cooling", "off"))
            .RuleFor(x => x.Thermostats, f => _thermostatFaker.Generate(f.Random.Int(1, 3)))
            .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset())
            .RuleFor(x => x.RelayConnectionActive, f => f.Random.Bool())
            .RuleFor(x => x.ThermostatsConnectionActive, f => f.Random.Bool())
            .RuleFor(x => x.OutdoorTemperature, f => f.Random.Double(5, 35))
            .RuleFor(x => x.OutdoorHumidity, f => f.Random.Double(30, 90));
    }

    [Fact]
    public void Generate_AllFeaturesEnabled_GeneratesAllMessages()
    {
        // Arrange
        var configuration = CreateConfiguration(
            true,
            true,
            true,
            true,
            true,
            true,
            true,
            true
        );

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(configuration);

        var apartment = _apartmentFaker.Generate();
        var thermostatCount = apartment.Thermostats.Count();

        var sut = CreateAutoDiscoveryGenerator();

        // Act
        var result = sut.Generate(apartment).ToList();

        // Assert
        var expectedMessageCount = 3 + thermostatCount * 5; // 3 apartment + (5 sensors per thermostat)
        Assert.Equal(expectedMessageCount, result.Count);
        Assert.All(result, msg =>
        {
            Assert.NotNull(msg);
            Assert.NotEmpty(msg.Topic);
            Assert.NotEmpty(msg.Payload);
        });
    }

    [Fact]
    public void Generate_OnlyApartmentSystemModeEnabled_GeneratesOnlyApartmentMessage()
    {
        // Arrange
        var configuration = CreateConfiguration(
            true,
            false,
            false,
            false,
            false,
            false,
            false,
            false
        );

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(configuration);

        var apartment = _apartmentFaker.Generate();

        var sut = CreateAutoDiscoveryGenerator();

        // Act
        var result = sut.Generate(apartment).ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains("apartment", result[0].Topic);
    }

    [Fact]
    public void Generate_OnlyThermostatTemperatureEnabled_GeneratesOnlyTemperatureMessages()
    {
        // Arrange
        var configuration = CreateConfiguration(
            false,
            false,
            false,
            false,
            true,
            false,
            false,
            false
        );

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(configuration);

        var apartment = _apartmentFaker.Generate();
        var thermostatCount = apartment.Thermostats.Count();

        var sut = CreateAutoDiscoveryGenerator();

        // Act
        var result = sut.Generate(apartment).ToList();

        // Assert
        Assert.Equal(thermostatCount, result.Count);
        Assert.All(result, msg => Assert.Contains("sensor", msg.Topic));
    }

    [Fact]
    public void Generate_AllFeaturesDisabled_GeneratesNoMessages()
    {
        // Arrange
        var configuration = CreateConfiguration(
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
        );

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(configuration);

        var apartment = _apartmentFaker.Generate();

        var sut = CreateAutoDiscoveryGenerator();

        // Act
        var result = sut.Generate(apartment).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Generate_ApartmentWithNoThermostats_GeneratesOnlyApartmentMessage()
    {
        // Arrange
        var configuration = CreateConfiguration(
            true,
            true,
            true,
            true,
            true,
            true,
            true,
            true
        );

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(configuration);

        var apartment = new Apartment
        {
            SystemMode = "heating",
            Thermostats = Array.Empty<Thermostat>(),
            LastUpdate = DateTimeOffset.UtcNow,
            RelayConnectionActive = true,
            ThermostatsConnectionActive = true
        };

        var sut = CreateAutoDiscoveryGenerator();

        // Act
        var result = sut.Generate(apartment).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, msg => Assert.Contains("apartment", msg.Topic));
    }

    [Fact]
    public void Generate_MultipleThermostats_GeneratesMessagesForEach()
    {
        // Arrange
        var configuration = CreateConfiguration(
            false,
            false,
            false,
            false,
            true,
            false,
            false,
            true
        );

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(configuration);

        var thermostats = _thermostatFaker.Generate(5);
        var apartment = new Apartment
        {
            SystemMode = "heating",
            Thermostats = thermostats,
            LastUpdate = DateTimeOffset.UtcNow,
            RelayConnectionActive = true,
            ThermostatsConnectionActive = true
        };

        var sut = CreateAutoDiscoveryGenerator();

        // Act
        var result = sut.Generate(apartment).ToList();

        // Assert
        Assert.Equal(10, result.Count); // 5 thermostats * 2 sensors (temperature + climate)
    }

    [Fact]
    public void Generate_ClimateEnabled_GeneratesClimateMessages()
    {
        // Arrange
        var configuration = CreateConfiguration(
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            true
        );

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(configuration);

        var apartment = _apartmentFaker.Generate();
        var thermostatCount = apartment.Thermostats.Count();

        var sut = CreateAutoDiscoveryGenerator();

        // Act
        var result = sut.Generate(apartment).ToList();

        // Assert
        Assert.Equal(thermostatCount, result.Count);
        Assert.All(result, msg =>
        {
            Assert.Contains("climate", msg.Topic);
            var payload = JsonSerializer.Deserialize<ClimateAutoDiscovery>(msg.Payload);
            Assert.NotNull(payload);
            Assert.Equal(0.5, payload.TemperatureStep);
            Assert.Equal(0.1, payload.Precision);
        });
    }

    [Fact]
    public void Generate_AllThermostatSensorsEnabled_GeneratesAllSensorMessages()
    {
        // Arrange
        var configuration = CreateConfiguration(
            false,
            false,
            false,
            true,
            true,
            true,
            true,
            false
        );

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(configuration);

        var thermostat = _thermostatFaker.Generate();
        var apartment = new Apartment
        {
            SystemMode = "heating",
            Thermostats = new[] { thermostat },
            LastUpdate = DateTimeOffset.UtcNow,
            RelayConnectionActive = true,
            ThermostatsConnectionActive = true
        };

        var sut = CreateAutoDiscoveryGenerator();

        // Act
        var result = sut.Generate(apartment).ToList();

        // Assert
        Assert.Equal(4, result.Count); // 4 sensors: humidity, temperature, target temp, system mode
        Assert.All(result, msg => Assert.Contains("sensor", msg.Topic));
    }

    private AutoDiscoveryGenerator CreateAutoDiscoveryGenerator()
    {
        return new AutoDiscoveryGenerator(
            _optionsMonitorMock.Object,
            _apartmentSystemModeSensor,
            _apartmentOutdoorTemperatureSensor,
            _apartmentOutdoorHumiditySensor,
            _thermostatHumiditySensor,
            _thermostatTemperatureSensor,
            _thermostatTargetTemperatureSensor,
            _thermostatSystemModeSensor,
            _thermostatClimate
        );
    }

    private static HomeAssistantAutoDiscoveryConfiguration CreateConfiguration(
        bool apartmentSystemModeEnabled,
        bool apartmentOutdoorTemperatureEnabled,
        bool apartmentOutdoorHumidityEnabled,
        bool thermostatHumidityEnabled,
        bool thermostatTemperatureEnabled,
        bool thermostatTargetTemperatureEnabled,
        bool thermostatSystemModeEnabled,
        bool climateEnabled)
    {
        return new HomeAssistantAutoDiscoveryConfiguration
        {
            SbmTopic = SbmTopic,
            HomeAssistantTopic = HomeAssistantTopic,
            ThermostatTemperatureDiscoveryEnabled = thermostatTemperatureEnabled,
            ThermostatTargetTemperatureDiscoveryEnabled = thermostatTargetTemperatureEnabled,
            ThermostatHumidityDiscoveryEnabled = thermostatHumidityEnabled,
            ThermostatSystemModeDiscoveryEnabled = thermostatSystemModeEnabled,
            ClimateDiscoveryEnabled = climateEnabled,
            ApartmentSystemModeDiscoveryEnabled = apartmentSystemModeEnabled,
            ApartmentOutdoorTemperatureDiscoveryEnabled = apartmentOutdoorTemperatureEnabled,
            ApartmentOutdoorHumidityDiscoveryEnabled = apartmentOutdoorHumidityEnabled
        };
    }
}