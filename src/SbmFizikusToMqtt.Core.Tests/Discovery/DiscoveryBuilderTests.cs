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
    public void Build_Climate_MapsUnrecognizedModeAndActionToNone()
    {
        // Arrange
        var sut = new DiscoveryBuilder(TestApartments.Topics, AllEnabled);

        // Act
        var thermostat = sut.Build(TestApartments.Create(thermostats: TestApartments.LivingRoom()))[1];

        // Assert
        // Home Assistant logs a warning for every value outside its modes and actions; "None" resets them to unknown.
        var climate = Assert.Single(thermostat.Components, x => x.Key == "climate").Config;
        Assert.Equal("{{ {'heating': 'heat', 'cooling': 'cool'}.get(value_json.system_mode, 'None') }}",
            climate["mode_state_template"]!.GetValue<string>());
        Assert.Equal(
            "{{ value_json.system_mode if value_json.system_mode in ['heating', 'cooling', 'idle'] else 'None' }}",
            climate["action_template"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, true, true)]
    public void Build_ApartmentSensors_LinksThermostatsOnlyToAnExistingApartmentDevice(bool systemMode,
        bool outdoorTemperature, bool outdoorHumidity, bool expectViaDevice)
    {
        // Arrange
        var sut = new DiscoveryBuilder(TestApartments.Topics, new DiscoveryOptions
        {
            ApartmentSystemModeEnabled = systemMode,
            ApartmentOutdoorTemperatureEnabled = outdoorTemperature,
            ApartmentOutdoorHumidityEnabled = outdoorHumidity
        });

        // Act
        var devices = sut.Build(TestApartments.Create(thermostats: TestApartments.LivingRoom()));

        // Assert
        Assert.Equal(expectViaDevice, devices[0].Components.Count > 0);
        Assert.Equal(expectViaDevice ? "sbm_fizikus_apartment" : null,
            devices[1].Device["via_device"]?.GetValue<string>());
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