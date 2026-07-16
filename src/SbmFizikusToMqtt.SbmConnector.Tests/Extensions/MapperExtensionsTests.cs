using SbmFizikusToMqtt.SbmConnector.Extensions;
using SbmFizikusToMqtt.SbmConnector.Models.Response;

namespace SbmFizikusToMqtt.SbmConnector.Tests.Extensions;

public class MapperExtensionsTests
{
    [Fact]
    public void ToApartment_WithHeatingMode_ShouldMapCorrectly()
    {
        // Arrange
        var response = CreateSbmApartmentInfoResponse();

        // Act
        var result = response.ToApartment();

        // Assert
        Assert.Equal("heating", result.SystemMode);
        Assert.NotNull(result.Thermostats);
        Assert.Equal(response.Thermostats.Count(), result.Thermostats.Count());
        Assert.Equal(response.LastStateUpdate, result.LastUpdate);
        Assert.Equal(response.CommunicationActiveRelayModule, result.RelayConnectionActive);
        Assert.Equal(response.CommunicationActiveThermostats, result.ThermostatsConnectionActive);
    }

    [Fact]
    public void ToApartment_WithCoolingMode_ShouldMapCorrectly()
    {
        // Arrange
        var response = CreateSbmApartmentInfoResponse((int)OperationMode.Cooling);

        // Act
        var result = response.ToApartment();

        // Assert
        Assert.Equal("cooling", result.SystemMode);
    }

    [Fact]
    public void ToApartment_WithUnknownMode_ShouldMapToUnknown()
    {
        // Arrange
        var response = CreateSbmApartmentInfoResponse(999);

        // Act
        var result = response.ToApartment();

        // Assert
        Assert.Equal("unknown", result.SystemMode);
    }

    [Fact]
    public void ToApartment_WithMultipleThermostats_ShouldMapAllThermostats()
    {
        // Arrange
        var response = CreateSbmApartmentInfoResponse(
            thermostats: new List<SbmApartmentInfoResponse.Thermostat>
            {
                CreateThermostat(1, "Living Room"),
                CreateThermostat(2, "Bedroom"),
                CreateThermostat(3, "Kitchen")
            });

        // Act
        var result = response.ToApartment();

        // Assert
        var thermostatList = result.Thermostats.ToList();
        Assert.Equal(3, thermostatList.Count);
        Assert.Equal("Living Room", thermostatList[0].Name);
        Assert.Equal("Bedroom", thermostatList[1].Name);
        Assert.Equal("Kitchen", thermostatList[2].Name);
    }

    [Fact]
    public void ToApartment_WithEmptyThermostatsList_ShouldHandleGracefully()
    {
        // Arrange
        var response = CreateSbmApartmentInfoResponse(thermostats: new List<SbmApartmentInfoResponse.Thermostat>());

        // Act
        var result = response.ToApartment();

        // Assert
        Assert.Empty(result.Thermostats);
    }

    [Fact]
    public void ToApartment_ThermostatMapping_ShouldMapAllProperties()
    {
        // Arrange
        var expectedTemperature = 22.5;
        var expectedHumidity = 45.0;
        var expectedTargetTemperature = 21.0;
        var expectedDewPoint = 12.3;
        var expectedActive = true;
        var lastUpdate = DateTimeOffset.UtcNow;

        var response = CreateSbmApartmentInfoResponse(
            thermostats: new List<SbmApartmentInfoResponse.Thermostat>
            {
                new()
                {
                    Id = 1,
                    ThermostatNo = 1,
                    Name = "Test Thermostat",
                    ConfigUpdatedByWebapp = false,
                    TemperatureSetpointDegC = expectedTargetTemperature,
                    CondensationRiskLevel = 0.5,
                    MeasuredTempDegC = expectedTemperature,
                    MeasuredHumPerc = expectedHumidity,
                    DewPointDegC = expectedDewPoint,
                    Active = expectedActive,
                    LastStateUpdate = lastUpdate
                }
            });

        // Act
        var result = response.ToApartment();
        var thermostat = result.Thermostats.First();

        // Assert
        Assert.Equal(1, thermostat.Id);
        Assert.Equal("Test Thermostat", thermostat.Name);
        Assert.Equal(expectedTemperature, thermostat.Temperature);
        Assert.Equal(expectedHumidity, thermostat.Humidity);
        Assert.Equal(expectedTargetTemperature, thermostat.TargetTemperature);
        Assert.Equal(expectedDewPoint, thermostat.DewPoint);
        Assert.Equal(expectedActive, thermostat.Active);
        Assert.Equal(lastUpdate, thermostat.LastUpdate);
    }

    [Fact]
    public void ToApartment_ThermostatMapping_ShouldHandleNullName()
    {
        // Arrange
        var response = CreateSbmApartmentInfoResponse(
            thermostats: new List<SbmApartmentInfoResponse.Thermostat>
            {
                new()
                {
                    Id = 1,
                    ThermostatNo = 1,
                    Name = null!,
                    ConfigUpdatedByWebapp = false,
                    TemperatureSetpointDegC = 19.0,
                    CondensationRiskLevel = 0.5,
                    MeasuredTempDegC = 20.0,
                    MeasuredHumPerc = 50.0,
                    DewPointDegC = 10.0,
                    Active = true,
                    LastStateUpdate = DateTimeOffset.UtcNow
                }
            });

        // Act
        var result = response.ToApartment();
        var thermostat = result.Thermostats.First();

        // Assert
        Assert.Null(thermostat.Name);
    }

    [Fact]
    public void ToApartment_ShouldPreserveConnectionStatus()
    {
        // Arrange
        var response = CreateSbmApartmentInfoResponse(
            relayActive: true,
            thermostatsActive: false);

        // Act
        var result = response.ToApartment();

        // Assert
        Assert.True(result.RelayConnectionActive);
        Assert.False(result.ThermostatsConnectionActive);
    }

    [Fact]
    public void ToApartment_ShouldPreserveLastUpdateTime()
    {
        // Arrange
        var expectedLastUpdate = new DateTimeOffset(2026, 2, 15, 12, 0, 0, TimeSpan.FromHours(1));
        var response = CreateSbmApartmentInfoResponse(lastUpdate: expectedLastUpdate);

        // Act
        var result = response.ToApartment();

        // Assert
        Assert.Equal(expectedLastUpdate, result.LastUpdate);
    }

    [Fact]
    public void ToApartment_WithOutdoorWeather_ShouldMapOutdoorFields()
    {
        // Arrange
        var response = CreateSbmApartmentInfoResponse();
        const double expectedTemperature = 12.5;
        const double expectedHumidity = 68.0;

        // Act
        var result = response.ToApartment(expectedTemperature, expectedHumidity);

        // Assert
        Assert.Equal(expectedTemperature, result.OutdoorTemperature);
        Assert.Equal(expectedHumidity, result.OutdoorHumidity);
    }

    [Fact]
    public void ToApartment_WithoutOutdoorWeather_ShouldLeaveOutdoorFieldsNull()
    {
        // Arrange
        var response = CreateSbmApartmentInfoResponse();

        // Act
        var result = response.ToApartment();

        // Assert
        Assert.Null(result.OutdoorTemperature);
        Assert.Null(result.OutdoorHumidity);
    }

    private static SbmApartmentInfoResponse CreateSbmApartmentInfoResponse(
        int operationMode = (int)OperationMode.Heating,
        List<SbmApartmentInfoResponse.Thermostat>? thermostats = null,
        bool relayActive = true,
        bool thermostatsActive = true,
        DateTimeOffset? lastUpdate = null)
    {
        return new SbmApartmentInfoResponse
        {
            Name = "Test Apartment",
            ForwardWaterTemperature1DegC = 45.0,
            ReturningWaterTemperature1DegC = 40.0,
            FlowRateLiterPerHour1 = 100.0,
            ForwardWaterTemperature2DegC = 45.0,
            ReturningWaterTemperature2DegC = 40.0,
            FlowRateLiterPerHour2 = 100.0,
            HeatingHeatQuantity1KWh = 100.0,
            CoolingHeatQuantity1KWh = 0.0,
            HeatingCoolingVolumeMeter1M3 = 10.0,
            HeatingHeatQuantity2KWh = 100.0,
            CoolingHeatQuantity2KWh = 0.0,
            HeatingCoolingVolumeMeter2M3 = 10.0,
            HotWaterHeatingQuantity1KWh = 50.0,
            HotWaterVolumeMeter1M3 = 5.0,
            HotWaterHeatingQuantity2KWh = 50.0,
            HotWaterVolumeMeter2M3 = 5.0,
            ColdWaterVolumeMeter1M3 = 20.0,
            ColdWaterVolumeMeter2M3 = 20.0,
            LastMeterSynchronisation = "2026-02-15 12:00:00",
            LastStateUpdate = lastUpdate ?? DateTimeOffset.UtcNow,
            FwVer = "1.0.0",
            OperationMode = operationMode,
            DewPointOffinitDegC = 12.0,
            DehumidificationEquipment = 1,
            Thermostats = thermostats ?? new List<SbmApartmentInfoResponse.Thermostat> { CreateThermostat() },
            CommunicationActiveRelayModule = relayActive,
            CommunicationActiveThermostats = thermostatsActive
        };
    }

    private static SbmApartmentInfoResponse.Thermostat CreateThermostat(
        int id = 1,
        string name = "Test Thermostat")
    {
        return new SbmApartmentInfoResponse.Thermostat
        {
            Id = id,
            ThermostatNo = id,
            Name = name,
            ConfigUpdatedByWebapp = false,
            TemperatureSetpointDegC = 21.0,
            CondensationRiskLevel = 0.5,
            Active = true,
            MeasuredTempDegC = 22.0,
            MeasuredHumPerc = 45.0,
            DewPointDegC = 12.0,
            LastStateUpdate = DateTimeOffset.UtcNow
        };
    }
}