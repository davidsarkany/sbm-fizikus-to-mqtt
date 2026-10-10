using SbmFizikusToMqtt.Core.Discovery;
using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Tests.TestSupport;

namespace SbmFizikusToMqtt.Core.Tests.Discovery;

public sealed class DiscoveryBuilderTests
{
    private static readonly DiscoveryOptions AllEnabled = new()
    {
        ApartmentSystemModeEnabled = true,
        ApartmentOutdoorTemperatureEnabled = true,
        ApartmentOutdoorHumidityEnabled = true,
        ThermostatTemperatureEnabled = true,
        ThermostatTargetTemperatureEnabled = true,
        ThermostatHumidityEnabled = true,
        ThermostatSystemModeEnabled = true,
        ClimateEnabled = true,
        SoftwareVersion = "3.0.0-test"
    };

    [Fact]
    public void ToDeviceMessage_AllEntitiesEnabled_MatchesV3WireFormat()
    {
        // Arrange
        var sut = new DiscoveryBuilder(TestApartments.Topics, AllEnabled);

        // Act
        var messages = sut.Build(TestApartments.Create()).Select(sut.ToDeviceMessage);

        // Assert
        Snapshot.Match(MqttMessageFormatter.Format(messages));
    }

    [Fact]
    public void ToDeviceMessage_DefaultOptions_PublishesClimateAndRemovesDisabledComponents()
    {
        // Arrange
        var sut = new DiscoveryBuilder(TestApartments.Topics, new DiscoveryOptions { SoftwareVersion = "3.0.0-test" });

        // Act
        var messages = sut.Build(TestApartments.Create(thermostats: TestApartments.LivingRoom()))
            .Select(sut.ToDeviceMessage);

        // Assert
        Snapshot.Match(MqttMessageFormatter.Format(messages));
    }

    [Fact]
    public void Build_AnyOptions_KeepsV2UniqueIds()
    {
        // Arrange
        var sut = new DiscoveryBuilder(TestApartments.Topics, AllEnabled);

        // Act
        var uniqueIds = sut.Build(TestApartments.Create(thermostats: TestApartments.LivingRoom()))
            .SelectMany(x => x.Components)
            .Select(x => x.Config["unique_id"]!.GetValue<string>())
            .Order(StringComparer.Ordinal);

        // Assert
        Assert.Equal(
        [
            "sbm_fizikus-1763_climate",
            "sbm_fizikus-1763_humidity",
            "sbm_fizikus-1763_system_mode",
            "sbm_fizikus-1763_temperature",
            "sbm_fizikus-1763_temperature_target",
            "sbm_fizikus-apartment_info_outdoor_humidity",
            "sbm_fizikus-apartment_info_outdoor_temperature",
            "sbm_fizikus-apartment_info_system_mode"
        ], uniqueIds);
    }
}