using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Publishing;
using SbmFizikusToMqtt.Core.Tests.TestSupport;

namespace SbmFizikusToMqtt.Core.Tests.Publishing;

public sealed class StatePayloadMapperTests
{
    private readonly StatePayloadMapper _sut = new(TestApartments.Topics);

    [Fact]
    public void Map_ConnectedApartmentWithOutdoorData_MatchesV3WireFormat()
    {
        // Arrange
        var apartment = TestApartments.Create();

        // Act
        var messages = _sut.Map(apartment);

        // Assert
        Snapshot.Match(MqttMessageFormatter.Format(messages));
    }

    [Fact]
    public void Map_DisconnectedApartmentWithoutOutdoorData_OmitsOutdoorFields()
    {
        // Arrange
        var apartment = TestApartments.Create(connected: false, withOutdoorData: false,
            systemMode: SystemMode.Cooling);

        // Act
        var messages = _sut.Map(apartment);

        // Assert
        Snapshot.Match(MqttMessageFormatter.Format(messages));
    }

    [Fact]
    public void Map_UnnamedThermostat_DevicePayloadKeepsV2FieldOrderAndNullName()
    {
        // Arrange
        var apartment = TestApartments.Create(thermostats: TestApartments.Unnamed());

        // Act
        var deviceMessage = _sut.Map(apartment).Single(x => x.Topic == "sbm_fizikus/devices/1764");

        // Assert
        Assert.Equal(
            "{\"id\":1764,\"name\":null,\"temperature\":24.6,\"humidity\":55,\"target_temperature\":22.5," +
            "\"system_mode\":\"idle\",\"last_update\":\"2026-10-10T12:45:40+00:00\"}",
            deviceMessage.Payload);
    }

    [Fact]
    public void Map_ThermostatWithoutReadings_PublishesNullValues()
    {
        // Arrange
        var apartment = TestApartments.Create(thermostats: TestApartments.LivingRoom() with
        {
            Temperature = null,
            Humidity = null,
            TargetTemperature = null,
            LastUpdate = null
        });

        // Act
        var deviceMessage = _sut.Map(apartment).Single(x => x.Topic == "sbm_fizikus/devices/1763");

        // Assert
        Assert.Equal(
            "{\"id\":1763,\"name\":\"living-room\",\"temperature\":null,\"humidity\":null," +
            "\"target_temperature\":null,\"system_mode\":\"heating\",\"last_update\":null}",
            deviceMessage.Payload);
    }

    [Fact]
    public void Map_ApartmentWithoutLastUpdate_OmitsIt()
    {
        // Arrange
        var apartment = TestApartments.Create(withOutdoorData: false) with { LastUpdate = null };

        // Act
        var info = _sut.Map(apartment).Single(x => x.Topic == "sbm_fizikus/apartment_info");

        // Assert
        Assert.Equal("{\"system_mode\":\"heating\",\"connected\":true}", info.Payload);
    }

    [Theory]
    [InlineData(true, SystemMode.Heating, "heating")]
    [InlineData(true, SystemMode.Cooling, "cooling")]
    [InlineData(true, SystemMode.Unknown, "unknown")]
    [InlineData(false, SystemMode.Heating, "idle")]
    public void Map_Thermostat_SystemModeReflectsActivity(bool active, SystemMode systemMode, string expected)
    {
        // Arrange
        var apartment = TestApartments.Create(systemMode: systemMode,
            thermostats: TestApartments.LivingRoom(active));

        // Act
        var deviceMessage = _sut.Map(apartment).Single(x => x.Topic == "sbm_fizikus/devices/1763");

        // Assert
        Assert.Contains($"\"system_mode\":\"{expected}\"", deviceMessage.Payload);
    }
}