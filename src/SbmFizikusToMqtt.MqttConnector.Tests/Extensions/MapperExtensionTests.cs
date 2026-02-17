using Bogus;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Extensions;

namespace SbmFizikusToMqtt.MqttConnector.Tests.Extensions;

public sealed class MapperExtensionTests
{
    private static readonly Faker<Thermostat> ThermostatFaker = new Faker<Thermostat>()
        .RuleFor(x => x.Id, f => f.Random.Int(1, 1000))
        .RuleFor(x => x.Name, f => f.Name.FirstName())
        .RuleFor(x => x.Temperature, f => f.Random.Double(15, 30))
        .RuleFor(x => x.Humidity, f => f.Random.Double(30, 70))
        .RuleFor(x => x.TargetTemperature, f => f.Random.Double(18, 25))
        .RuleFor(x => x.DewPoint, f => f.Random.Double(5, 15))
        .RuleFor(x => x.Active, f => f.Random.Bool())
        .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset());

    private static readonly Faker<Apartment> ApartmentFaker = new Faker<Apartment>()
        .RuleFor(x => x.SystemMode, f => f.PickRandom("heat", "cool", "auto"))
        .RuleFor(x => x.Thermostats, f => ThermostatFaker.Generate(3))
        .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset())
        .RuleFor(x => x.RelayConnectionActive, f => f.Random.Bool())
        .RuleFor(x => x.ThermostatsConnectionActive, f => f.Random.Bool());

    [Fact]
    public void ToMqttThermostat_ActiveThermostat_ReturnsSystemMode()
    {
        // Arrange
        var thermostat = ThermostatFaker
            .RuleFor(x => x.Active, true)
            .Generate();
        const string systemMode = "heat";

        // Act
        var result = thermostat.ToMqttThermostat(systemMode);

        // Assert
        Assert.Equal(thermostat.Id, result.Id);
        Assert.Equal(thermostat.Name, result.Name);
        Assert.Equal(thermostat.Temperature, result.Temperature);
        Assert.Equal(thermostat.Humidity, result.Humidity);
        Assert.Equal(thermostat.TargetTemperature, result.TargetTemperature);
        Assert.Equal(thermostat.LastUpdate, result.LastUpdate);
        Assert.Equal(systemMode, result.SystemMode);
    }

    [Fact]
    public void ToMqttThermostat_InactiveThermostat_ReturnsIdleMode()
    {
        // Arrange
        var thermostat = ThermostatFaker
            .RuleFor(x => x.Active, false)
            .Generate();
        const string systemMode = "heat";

        // Act
        var result = thermostat.ToMqttThermostat(systemMode);

        // Assert
        Assert.Equal(thermostat.Id, result.Id);
        Assert.Equal(thermostat.Name, result.Name);
        Assert.Equal(thermostat.Temperature, result.Temperature);
        Assert.Equal(thermostat.Humidity, result.Humidity);
        Assert.Equal(thermostat.TargetTemperature, result.TargetTemperature);
        Assert.Equal(thermostat.LastUpdate, result.LastUpdate);
        Assert.Equal("idle", result.SystemMode);
    }

    [Theory]
    [InlineData("heat")]
    [InlineData("cool")]
    [InlineData("auto")]
    public void ToMqttThermostat_ActiveThermostatWithDifferentModes_ReturnsCorrectSystemMode(string systemMode)
    {
        // Arrange
        var thermostat = ThermostatFaker
            .RuleFor(x => x.Active, true)
            .Generate();

        // Act
        var result = thermostat.ToMqttThermostat(systemMode);

        // Assert
        Assert.Equal(systemMode, result.SystemMode);
    }

    [Fact]
    public void ToMqttApartment_ValidApartment_MapsCorrectly()
    {
        // Arrange
        var apartment = ApartmentFaker.Generate();

        // Act
        var result = apartment.ToMqttApartment();

        // Assert
        Assert.Equal(apartment.SystemMode, result.SystemMode);
        Assert.Equal(apartment.LastUpdate, result.LastUpdate);
    }

    [Theory]
    [InlineData("heat")]
    [InlineData("cool")]
    [InlineData("auto")]
    public void ToMqttApartment_DifferentSystemModes_MapsCorrectly(string systemMode)
    {
        // Arrange
        var apartment = ApartmentFaker
            .RuleFor(x => x.SystemMode, systemMode)
            .Generate();

        // Act
        var result = apartment.ToMqttApartment();

        // Assert
        Assert.Equal(systemMode, result.SystemMode);
    }
}