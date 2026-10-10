using SbmFizikusToMqtt.Core.Domain;

namespace SbmFizikusToMqtt.Core.Tests.Domain;

public sealed class TargetTemperatureLimitsTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(21.5)]
    [InlineData(23)]
    [InlineData(35)]
    public void IsValid_HalfDegreeStepWithinRange_ReturnsTrue(double temperature)
    {
        // Arrange & Act
        var result = TargetTemperatureLimits.IsValid(temperature);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(4.5)]
    [InlineData(35.5)]
    [InlineData(21.3)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void IsValid_OutOfRangeOrOffStepOrNotFinite_ReturnsFalse(double temperature)
    {
        // Arrange & Act
        var result = TargetTemperatureLimits.IsValid(temperature);

        // Assert
        Assert.False(result);
    }
}