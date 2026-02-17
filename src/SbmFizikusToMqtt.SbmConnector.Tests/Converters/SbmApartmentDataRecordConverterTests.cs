using System.Text.Json;
using SbmFizikusToMqtt.SbmConnector.Converters;
using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Tests.Converters;

public class SbmApartmentDataRecordConverterTests
{
    private readonly JsonSerializerOptions _options;

    public SbmApartmentDataRecordConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new SbmApartmentDataRecordConverter());
    }

    [Fact]
    public void Read_ValidJsonArray_ReturnsCorrectObject()
    {
        // Arrange
        var json = "[1, 2, \"Test Apartment\", true]";

        // Act
        var result = JsonSerializer.Deserialize<SbmApartmentListResponse>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.BuildingId);
        Assert.Equal(2, result.ApartmentId);
        Assert.Equal("Test Apartment", result.Name);
        Assert.True(result.IsOnline);
    }

    [Fact]
    public void Write_Object_SerializesToCorrectJsonArray()
    {
        // Arrange
        var obj = new SbmApartmentListResponse
        {
            BuildingId = 10,
            ApartmentId = 20,
            Name = "AptName",
            IsOnline = false
        };

        // Act
        var json = JsonSerializer.Serialize(obj, _options);

        // Assert
        Assert.Equal("[10,20,\"AptName\",false]", json);
    }

    [Theory]
    [InlineData("[1, \"wrongType\", \"Name\", true]")]
    [InlineData("[1, 2, null, true]")]
    [InlineData("[1, 2, \"Name\", 123]")]
    [InlineData("{\"not\":\"an array\"}")]
    public void Read_InvalidJson_ThrowsJsonException(string invalidJson)
    {
        // Arrange - invalidJson is provided as parameter

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<SbmApartmentListResponse>(invalidJson, _options));
    }

    [Fact]
    public void Read_EmptyArray_ThrowsJsonException()
    {
        // Arrange
        var json = "[]";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<SbmApartmentListResponse>(json, _options));
    }
}