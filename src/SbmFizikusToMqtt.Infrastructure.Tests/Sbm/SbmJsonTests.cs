using System.Text.Json;
using SbmFizikusToMqtt.Infrastructure.Sbm.Json;
using SbmFizikusToMqtt.Infrastructure.Sbm.Models;

namespace SbmFizikusToMqtt.Infrastructure.Tests.Sbm;

public sealed class SbmJsonTests
{
    private static readonly JsonSerializerOptions DateOptions = new() { Converters = { new SbmDateTimeOffsetConverter() } };

    private static readonly JsonSerializerOptions NullableDateOptions =
        new() { Converters = { new SbmNullableDateTimeOffsetConverter() } };

    [Theory]
    [InlineData("2026-10-10 12:45:11.578277", "2026-10-10T12:45:11.5782770+00:00")]
    [InlineData("2026-10-11 12:45:26.411281+00:00", "2026-10-11T12:45:26.4112810+00:00")]
    [InlineData("2026-10-10 14:45:11+02:00", "2026-10-10T14:45:11.0000000+02:00")]
    [InlineData("2026-10-10T12:45:11", "2026-10-10T12:45:11.0000000+00:00")]
    public void DateTimeOffsetConverter_SbmTimestamp_ReadsOffsetLessValuesAsUtc(string value, string expected)
    {
        // Arrange
        var json = JsonSerializer.Serialize(value);

        // Act
        var result = JsonSerializer.Deserialize<DateTimeOffset>(json, DateOptions);

        // Assert
        Assert.Equal(expected, result.ToString("O"));
    }

    [Theory]
    [InlineData("\"None\"")]
    [InlineData("null")]
    public void DateTimeOffsetConverter_InvalidTimestamp_ThrowsJsonException(string json)
    {
        // Arrange & Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset>(json, DateOptions));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"None\"")]
    [InlineData("\"\"")]
    public void NullableDateTimeOffsetConverter_MissingTimestamp_ReturnsNull(string json)
    {
        // Arrange & Act
        var result = JsonSerializer.Deserialize<DateTimeOffset?>(json, NullableDateOptions);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void NullableDateTimeOffsetConverter_SbmTimestamp_ReadsAsUtc()
    {
        // Arrange & Act
        var result = JsonSerializer.Deserialize<DateTimeOffset?>("\"2026-10-10 12:45:11\"", NullableDateOptions);

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 10, 10, 12, 45, 11, TimeSpan.Zero), result);
    }

    [Fact]
    public void NullableDateTimeOffsetConverter_InvalidTimestamp_ThrowsJsonException()
    {
        // Arrange & Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DateTimeOffset?>("\"yesterday\"", NullableDateOptions));
    }

    [Fact]
    public void BuildingAccessRights_RealFixture_ReadsPositionalFields()
    {
        // Arrange
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "sbm",
            "get_building_access_rights.json"));

        // Act
        var result = JsonSerializer.Deserialize(json, SbmJsonContext.Default.IReadOnlyListSbmBuildingAccessRight);

        // Assert
        var building = Assert.Single(result!);
        Assert.Equal(new SbmBuildingAccessRight
        {
            BuildingId = 24,
            OutdoorTemperature = 19.9,
            OutdoorHumidity = 49.9,
            Name = "T1"
        }, building);
    }

    [Fact]
    public void ApartmentAccessRights_RealFixture_ReadsPositionalFields()
    {
        // Arrange
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "sbm",
            "get_apartment_access_rights.json"));

        // Act
        var result = JsonSerializer.Deserialize(json, SbmJsonContext.Default.IReadOnlyListSbmApartmentAccessRight);

        // Assert
        var apartment = Assert.Single(result!);
        Assert.Equal(new SbmApartmentAccessRight { ApartmentId = 702, Name = "BP-TEST-001-101" }, apartment);
    }

    [Theory]
    [InlineData("[[24, 702]]", null)]
    [InlineData("[[24, 702, null, null]]", null)]
    [InlineData("[[24, 702, \"A\", true, \"appended\"]]", "A")]
    public void ApartmentAccessRights_OptionalElementsMissingOrAppended_ReadsIds(string json, string? name)
    {
        // Arrange & Act
        var result = JsonSerializer.Deserialize(json, SbmJsonContext.Default.IReadOnlyListSbmApartmentAccessRight);

        // Assert
        Assert.Equal(new SbmApartmentAccessRight { ApartmentId = 702, Name = name }, Assert.Single(result!));
    }

    [Theory]
    [InlineData("[[24, \"HU\", 1111, \"Budapest\", \"Street 1\", null, null, null]]")]
    [InlineData("[[24]]")]
    [InlineData("[[24, \"HU\", 1111, \"Budapest\", \"Street 1\", \"n/a\", \"n/a\", \"\", \"appended\"]]")]
    public void BuildingAccessRights_WithoutOutdoorSensorOrName_ReadsBuildingId(string json)
    {
        // Arrange & Act
        var result = JsonSerializer.Deserialize(json, SbmJsonContext.Default.IReadOnlyListSbmBuildingAccessRight);

        // Assert
        var building = Assert.Single(result!);
        Assert.Equal(24, building.BuildingId);
        Assert.Null(building.OutdoorTemperature);
        Assert.Null(building.OutdoorHumidity);
    }

    [Theory]
    [InlineData("[[24]]")]
    [InlineData("[[24, \"702\", \"A\", true]]")]
    [InlineData("[[24, 702.5]]")]
    [InlineData("[{\"buildingId\": 24}]")]
    public void ApartmentAccessRights_MalformedArray_ThrowsJsonException(string json)
    {
        // Arrange & Act & Assert
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize(json, SbmJsonContext.Default.IReadOnlyListSbmApartmentAccessRight));
    }

    [Fact]
    public void ApartmentData_ThermostatWithoutReadings_ParsesWithNulls()
    {
        // Arrange: a thermostat that never reported, next to a working one.
        const string json = """
                            {"operationMode": 0, "lastStateUpdate": "None",
                             "communicationActiveRelayModule": true, "communicationActiveThermostats": true,
                             "thermostats": [
                               {"ID": 7, "name": "new", "active": null, "measuredTempDegC": null,
                                "measuredHumPerc": null, "temperatureSetpointDegC": null, "dewPointDegC": null,
                                "lastStateUpdate": "None"},
                               {"ID": 8, "name": "ok", "active": true, "measuredTempDegC": 21.5,
                                "measuredHumPerc": 40, "temperatureSetpointDegC": 22,
                                "lastStateUpdate": "2026-10-10 12:45:11"}]}
                            """;

        // Act
        var result = JsonSerializer.Deserialize(json, SbmJsonContext.Default.SbmApartmentDataResponse)!;

        // Assert
        Assert.Null(result.LastStateUpdate);
        Assert.Equal(new SbmThermostat { Id = 7, Name = "new" }, result.Thermostats[0]);
        Assert.Equal(21.5, result.Thermostats[1].MeasuredTemperature);
    }

    [Fact]
    public void ApartmentData_StringNumbers_AreAccepted()
    {
        // Arrange
        const string json = """
                            {"operationMode": "1", "lastStateUpdate": "2026-10-10 12:45:11",
                             "communicationActiveRelayModule": true, "communicationActiveThermostats": false,
                             "thermostats": [{"ID": "7", "name": null, "active": true, "measuredTempDegC": "21.5",
                               "measuredHumPerc": 40, "temperatureSetpointDegC": 22, "dewPointDegC": 8,
                               "lastStateUpdate": "2026-10-10 12:45:11"}]}
                            """;

        // Act
        var result = JsonSerializer.Deserialize(json, SbmJsonContext.Default.SbmApartmentDataResponse)!;

        // Assert
        Assert.Equal(1, result.OperationMode);
        Assert.Equal(7, result.Thermostats[0].Id);
        Assert.Equal(21.5, result.Thermostats[0].MeasuredTemperature);
    }
}