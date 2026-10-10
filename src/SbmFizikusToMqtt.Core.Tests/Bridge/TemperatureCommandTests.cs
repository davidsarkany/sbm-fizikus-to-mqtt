using SbmFizikusToMqtt.Core.Bridge;

namespace SbmFizikusToMqtt.Core.Tests.Bridge;

public sealed class TemperatureCommandTests
{
    [Theory]
    [InlineData("{\"id\": 1763, \"value\": 21.5}")]
    [InlineData("{\"value\": 21.5}")]
    [InlineData("{\"ID\": 1763, \"Value\": 21.5}")]
    [InlineData("{\"id\": 1763, \"value\": 21.5, \"extra\": true}")]
    public void TryParse_ValidPayload_ReturnsCommand(string payload)
    {
        // Arrange & Act
        var result = TemperatureCommand.TryParse(1763, payload, out var command, out var error);

        // Assert
        Assert.True(result, error);
        Assert.Equal(new TemperatureCommand(1763, 21.5), command);
    }

    [Theory]
    [InlineData("not json", "payload is not valid JSON")]
    [InlineData("21.5", "payload must be a JSON object")]
    [InlineData("{\"id\": 1763}", "'value' is missing")]
    [InlineData("{\"value\": \"21.5\"}", "'value' must be a number")]
    [InlineData("{\"id\": \"1763\", \"value\": 21.5}", "'id' must be an integer")]
    [InlineData("{\"id\": 1764, \"value\": 21.5}", "payload id 1764 does not match topic id 1763")]
    [InlineData("{\"value\": 40}", "temperature must be between 5 and 35 in 0.5 steps")]
    [InlineData("{\"value\": 21.3}", "temperature must be between 5 and 35 in 0.5 steps")]
    public void TryParse_InvalidPayload_ReturnsError(string payload, string expectedError)
    {
        // Arrange & Act
        var result = TemperatureCommand.TryParse(1763, payload, out var command, out var error);

        // Assert
        Assert.False(result);
        Assert.Null(command);
        Assert.Equal(expectedError, error);
    }
}