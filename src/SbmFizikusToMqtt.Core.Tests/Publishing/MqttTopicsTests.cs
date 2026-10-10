using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Publishing;
using SbmFizikusToMqtt.Core.Tests.TestSupport;

namespace SbmFizikusToMqtt.Core.Tests.Publishing;

public sealed class MqttTopicsTests
{
    [Fact]
    public void Topics_DefaultConfiguration_MatchV2StateTopics()
    {
        // Arrange
        var sut = TestApartments.Topics;

        // Act & Assert
        Assert.Equal("sbm_fizikus/bridge/state", sut.BridgeState);
        Assert.Equal("sbm_fizikus/apartment_info", sut.ApartmentInfo);
        Assert.Equal("sbm_fizikus/devices/1763", sut.Device(1763));
        Assert.Equal("sbm_fizikus/devices/1763/set", sut.DeviceSet(1763));
        Assert.Equal("sbm_fizikus/devices/+/set", sut.DeviceSetFilter);
        Assert.Equal("homeassistant/status", sut.HomeAssistantStatus);
    }

    [Fact]
    public void NodeId_TopicWithSlashAndDot_ReplacesInvalidCharacters()
    {
        // Arrange
        var sut = new MqttTopics(new TopicOptions { SbmTopic = "home/sbm.fizikus", HomeAssistantTopic = "ha" });

        // Act & Assert
        Assert.Equal("home_sbm_fizikus", sut.NodeId);
        Assert.Equal("ha/device/home_sbm_fizikus_thermostat_7/config", sut.ThermostatDiscovery(7));
        Assert.Equal("ha/device/home_sbm_fizikus_apartment/config", sut.ApartmentDiscovery);
    }

    [Fact]
    public void TryParseDeviceSet_ValidTopic_ReturnsThermostatId()
    {
        // Arrange
        var sut = TestApartments.Topics;

        // Act
        var result = sut.TryParseDeviceSet("sbm_fizikus/devices/1763/set", out var thermostatId);

        // Assert
        Assert.True(result);
        Assert.Equal(1763, thermostatId);
    }

    [Theory]
    [InlineData("sbm_fizikus/devices/1763")]
    [InlineData("sbm_fizikus/devices//set")]
    [InlineData("sbm_fizikus/devices/abc/set")]
    [InlineData("sbm_fizikus/devices/-1/set")]
    [InlineData("sbm_fizikus/devices/1/2/set")]
    [InlineData("other/devices/1763/set")]
    [InlineData("sbm_fizikus/devices/99999999999/set")]
    public void TryParseDeviceSet_InvalidTopic_ReturnsFalse(string topic)
    {
        // Arrange
        var sut = TestApartments.Topics;

        // Act
        var result = sut.TryParseDeviceSet(topic, out _);

        // Assert
        Assert.False(result);
    }
}