using System.Text.Json;
using Bogus;
using SbmFizikusToMqtt.SbmConnector.Converters;

namespace SbmFizikusToMqtt.SbmConnector.Tests.Converters;

public class CustomDateTimeOffsetConverterTests
{
    private readonly JsonSerializerOptions _options;

    public CustomDateTimeOffsetConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new CustomDateTimeOffsetConverter());
    }

    [Fact]
    public void Write_ShouldSerializeDateTimeOffsetCorrectly()
    {
        // Arrange
        var faker = new Faker();
        var value = faker.Date.FutureOffset();
        var expected = value.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz");
        var expectedPlus = expected.Replace("+", "\\u002B");

        // Act
        var json = JsonSerializer.Serialize(value, _options);

        // Assert
        // Accept both + and \u002B for timezone offset
        Assert.True(json.Contains(expected) || json.Contains(expectedPlus),
            $"Serialized json '{json}' should contain either '{expected}' or '{expectedPlus}'");
    }

    [Fact]
    public void Read_ShouldDeserializeValidString()
    {
        // Arrange
        var faker = new Faker();
        var value = faker.Date.FutureOffset();
        var str = value.ToString("yyyy-MM-dd HH:mm:ss.ffffffzzz"); // space instead of T
        var json = $"\"{str}\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        // Assert
        Assert.Equal(value.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz"), result.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz"));
    }

    [Fact]
    public void Read_ShouldThrowOnNull()
    {
        // Arrange
        var json = "null";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset>(json, _options));
    }

    [Fact]
    public void Read_ShouldThrowOnInvalidFormat()
    {
        // Arrange
        var json = "\"not-a-date\"";

        // Act
        var ex = Record.Exception(() => JsonSerializer.Deserialize<DateTimeOffset>(json, _options));

        // Assert
        // Accept either JsonException or FormatException
        Assert.True(ex is JsonException || ex is FormatException,
            $"Expected JsonException or FormatException, got {ex?.GetType()}");
    }
}