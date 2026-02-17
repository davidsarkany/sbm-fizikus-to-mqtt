using System.Reflection;
using System.Text;
using System.Text.Json;
using SbmFizikusToMqtt.SbmConnector.Converters;
using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Tests.Converters;

public class SbmBuildingAccessRightsResponseConverterTests
{
    private readonly JsonSerializerOptions _options;

    public SbmBuildingAccessRightsResponseConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new SbmBuildingAccessRightsResponseConverter());
    }

    [Fact]
    public void CanDeserializeArrayToObject()
    {
        // Arrange
        var expected = new SbmGetBuildingAccessRightsResponse
        {
            BuildingId = 123,
            Country = "HU",
            PostalCode = 1111,
            City = "Budapest",
            Street = "Main st.",
            Temperature = 21.5,
            Humidity = 45.2,
            Name = "TestBuilding"
        };
        const string json = "[123,\"HU\",1111,\"Budapest\",\"Main st.\",21.5,45.2,\"TestBuilding\"]";

        // Act
        var result = JsonSerializer.Deserialize<SbmGetBuildingAccessRightsResponse>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.BuildingId, result.BuildingId);
        Assert.Equal(expected.Country, result.Country);
        Assert.Equal(expected.PostalCode, result.PostalCode);
        Assert.Equal(expected.City, result.City);
        Assert.Equal(expected.Street, result.Street);
        Assert.Equal(expected.Temperature, result.Temperature);
        Assert.Equal(expected.Humidity, result.Humidity);
        Assert.Equal(expected.Name, result.Name);
    }

    [Fact]
    public void CanSerializeObjectToArray()
    {
        // Arrange
        var obj = new SbmGetBuildingAccessRightsResponse
        {
            BuildingId = 123,
            Country = "HU",
            PostalCode = 1111,
            City = "Budapest",
            Street = "Main st.",
            Temperature = 21.5,
            Humidity = 45.2,
            Name = "TestBuilding"
        };
        const string expectedJson = "[123,\"HU\",1111,\"Budapest\",\"Main st.\",21.5,45.2,\"TestBuilding\"]";

        // Act
        var json = JsonSerializer.Serialize(obj, _options);

        // Assert
        Assert.Equal(expectedJson, json);
    }

    [Fact]
    public void ThrowsOnWrongArrayLength()
    {
        // Arrange
        var json = "[123,\"HU\",1111,\"Budapest\"]"; // Too short

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<SbmGetBuildingAccessRightsResponse>(json, _options));
    }

    [Fact]
    public void ThrowsOnWrongType()
    {
        // Arrange
        const string json = "[123,\"HU\",1111,\"Budapest\",\"Main st.\",21.5,45.2,123]"; // Name should be string

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<SbmGetBuildingAccessRightsResponse>(json, _options));
    }

    [Fact]
    public void Read_ThrowsJsonException_WhenNotStartArray()
    {
        // Arrange: JSON is an object, not an array
        var json = "{\"BuildingId\":1}";
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        // Move to first token
        reader.Read();

        // Act & Assert
        var converter = new SbmBuildingAccessRightsResponseConverter();
        JsonException? ex = null;
        try
        {
            converter.Read(ref reader, typeof(SbmGetBuildingAccessRightsResponse), _options);
        }
        catch (JsonException e)
        {
            ex = e;
        }

        Assert.NotNull(ex);
        Assert.Contains("Expected StartArray", ex.Message);
    }

    [Fact]
    public void ExtractNonEmptyString_ThrowsJsonException_WhenEmptyOrWhitespace()
    {
        // Arrange
        var emptyElement = JsonDocument.Parse("\"\"").RootElement;
        var whitespaceElement = JsonDocument.Parse("\"   \"").RootElement;

        // Act & Assert
        var method = typeof(SbmBuildingAccessRightsResponseConverter)
            .GetMethod("ExtractNonEmptyString", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        try
        {
            method.Invoke(null, new object[] { emptyElement, "TestField" });
            Assert.Fail("Expected exception was not thrown for empty string");
        }
        catch (TargetInvocationException ex1)
        {
            Assert.IsType<JsonException>(ex1.InnerException);
            Assert.Contains("cannot be empty or whitespace", ex1.InnerException.Message);
        }

        try
        {
            method.Invoke(null, new object[] { whitespaceElement, "TestField" });
            Assert.Fail("Expected exception was not thrown for whitespace string");
        }
        catch (TargetInvocationException ex2)
        {
            Assert.IsType<JsonException>(ex2.InnerException);
            Assert.Contains("cannot be empty or whitespace", ex2.InnerException.Message);
        }
    }
}