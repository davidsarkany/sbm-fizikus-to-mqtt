using System.Text;
using MQTTnet.Protocol;
using SbmFizikusToMqtt.Infrastructure.Mqtt;
using SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

namespace SbmFizikusToMqtt.Infrastructure.Tests.Mqtt;

public sealed class MqttGatewayUnitTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(16, 32)]
    [InlineData(32, 60)]
    [InlineData(60, 60)]
    public void ReconnectBackoff_Next_DoublesUpToOneMinute(int currentSeconds, int expectedSeconds)
    {
        // Arrange & Act
        var next = ReconnectBackoff.Next(TimeSpan.FromSeconds(currentSeconds));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), next);
    }

    [Fact]
    public async Task ClientOptions_Always_UseRetainedOfflineLastWill()
    {
        // Arrange
        await using var fixture = new GatewayFixture(InProcessBroker.Create());

        // Act
        var options = fixture.Gateway.ClientOptions;

        // Assert
        Assert.Equal("sbm_fizikus/bridge/state", options.WillTopic);
        Assert.Equal("{\"state\": \"offline\"}", Encoding.UTF8.GetString(options.WillPayload));
        Assert.True(options.WillRetain);
        Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, options.WillQualityOfServiceLevel);
        Assert.Equal("gateway-under-test", options.ClientId);
        Assert.True(options.CleanSession);
        Assert.Null(options.Credentials);
    }
}