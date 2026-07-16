using Microsoft.Extensions.Options;
using Moq;
using SbmFizikusToMqtt.SbmConnector.Configurations;
using SbmFizikusToMqtt.SbmConnector.Exceptions;
using SbmFizikusToMqtt.SbmConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Models.Response;
using SbmFizikusToMqtt.SbmConnector.Services;

namespace SbmFizikusToMqtt.SbmConnector.Tests.Services;

public class ApartmentServiceTests
{
    private readonly Mock<ISbmService> _sbmServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;

    public ApartmentServiceTests()
    {
        _sbmServiceMock = new Mock<ISbmService>();
        _tokenServiceMock = new Mock<ITokenService>();
    }

    private ApartmentService CreateSut(bool outdoorWeatherEnabled = false) =>
        new(_sbmServiceMock.Object, _tokenServiceMock.Object,
            Options.Create(new SbmConfiguration
            {
                Username = "test",
                Password = "test",
                BaseUrl = "https://test.com",
                OutdoorWeatherEnabled = outdoorWeatherEnabled
            }));

    [Fact]
    public async Task GetApartmentInfo_FirstCall_CachesApartmentIdAndReturnsApartment()
    {
        // Arrange
        var sut = CreateSut();
        var jwtToken = CreateTestToken();
        var buildingAccessRights = new[]
        {
            new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = 123,
                Country = "TestCountry",
                PostalCode = 12345,
                City = "TestCity",
                Street = "TestStreet",
                Temperature = 20.5,
                Humidity = 65.0,
                Name = "TestBuilding"
            }
        };
        var apartmentList = new[]
        {
            new SbmApartmentListResponse
            {
                BuildingId = 123,
                ApartmentId = 456,
                Name = "TestApartment",
                IsOnline = true
            }
        };
        var apartmentInfo = new SbmApartmentInfoResponse
        {
            Name = "TestApartment",
            ForwardWaterTemperature1DegC = 25.0,
            ReturningWaterTemperature1DegC = 23.0,
            FlowRateLiterPerHour1 = 100.0,
            ForwardWaterTemperature2DegC = 24.0,
            ReturningWaterTemperature2DegC = 22.0,
            FlowRateLiterPerHour2 = 95.0,
            HeatingHeatQuantity1KWh = 150.0,
            CoolingHeatQuantity1KWh = 75.0,
            HeatingCoolingVolumeMeter1M3 = 50.0,
            HeatingHeatQuantity2KWh = 140.0,
            CoolingHeatQuantity2KWh = 70.0,
            HeatingCoolingVolumeMeter2M3 = 45.0,
            HotWaterHeatingQuantity1KWh = 200.0,
            HotWaterVolumeMeter1M3 = 30.0,
            HotWaterHeatingQuantity2KWh = 190.0,
            HotWaterVolumeMeter2M3 = 28.0,
            ColdWaterVolumeMeter1M3 = 25.0,
            ColdWaterVolumeMeter2M3 = 23.0,
            LastMeterSynchronisation = "2024-01-01T00:00:00Z",
            FwVer = "1.0.0",
            DewPointOffinitDegC = 10.0,
            DehumidificationEquipment = 0,
            CommunicationActiveRelayModule = true,
            CommunicationActiveThermostats = true,
            OperationMode = 0, // Heating
            LastStateUpdate = DateTimeOffset.UtcNow,
            Thermostats = new List<SbmApartmentInfoResponse.Thermostat>
            {
                new()
                {
                    Id = 789,
                    ThermostatNo = 1,
                    Name = "Living Room",
                    ConfigUpdatedByWebapp = false,
                    MeasuredTempDegC = 22.5,
                    MeasuredHumPerc = 45.0,
                    TemperatureSetpointDegC = 23.0,
                    CondensationRiskLevel = 0.5,
                    DewPointDegC = 12.0,
                    Active = true,
                    LastStateUpdate = DateTimeOffset.UtcNow
                }
            }
        };

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingAccessRights);
        _sbmServiceMock.Setup(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentList);
        _sbmServiceMock.Setup(x => x.GetApartmentInfo(456, jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentInfo);

        // Act
        var result = await sut.GetApartmentInfo();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("heating", result.SystemMode);
        Assert.True(result.RelayConnectionActive);
        Assert.True(result.ThermostatsConnectionActive);
        Assert.Single(result.Thermostats);

        var thermostat = result.Thermostats.First();
        Assert.Equal(789, thermostat.Id);
        Assert.Equal("Living Room", thermostat.Name);
        Assert.Equal(22.5, thermostat.Temperature);
        Assert.Equal(45.0, thermostat.Humidity);
        Assert.Equal(23.0, thermostat.TargetTemperature);
        Assert.Equal(12.0, thermostat.DewPoint);
        Assert.True(thermostat.Active);

        // Verify the apartment ID caching behavior - all services should be called exactly once
        _tokenServiceMock.Verify(x => x.GetToken(It.IsAny<CancellationToken>()), Times.Once);
        _sbmServiceMock.Verify(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
        _sbmServiceMock.Verify(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
        _sbmServiceMock.Verify(x => x.GetApartmentInfo(456, jwtToken.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetApartmentInfo_SubsequentCall_UsesCache()
    {
        // Arrange - First call setup
        var sut = CreateSut();
        var jwtToken = CreateTestToken();
        var buildingAccessRights = new[]
        {
            new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = 123,
                Country = "TestCountry",
                PostalCode = 12345,
                City = "TestCity",
                Street = "TestStreet",
                Temperature = 20.5,
                Humidity = 65.0,
                Name = "TestBuilding"
            }
        };
        var apartmentList = new[]
        {
            new SbmApartmentListResponse
            {
                BuildingId = 123,
                ApartmentId = 456,
                Name = "TestApartment",
                IsOnline = true
            }
        };
        var apartmentInfo = new SbmApartmentInfoResponse
        {
            Name = "TestApartment",
            ForwardWaterTemperature1DegC = 25.0,
            ReturningWaterTemperature1DegC = 23.0,
            FlowRateLiterPerHour1 = 100.0,
            ForwardWaterTemperature2DegC = 24.0,
            ReturningWaterTemperature2DegC = 22.0,
            FlowRateLiterPerHour2 = 95.0,
            HeatingHeatQuantity1KWh = 150.0,
            CoolingHeatQuantity1KWh = 75.0,
            HeatingCoolingVolumeMeter1M3 = 50.0,
            HeatingHeatQuantity2KWh = 140.0,
            CoolingHeatQuantity2KWh = 70.0,
            HeatingCoolingVolumeMeter2M3 = 45.0,
            HotWaterHeatingQuantity1KWh = 200.0,
            HotWaterVolumeMeter1M3 = 30.0,
            HotWaterHeatingQuantity2KWh = 190.0,
            HotWaterVolumeMeter2M3 = 28.0,
            ColdWaterVolumeMeter1M3 = 25.0,
            ColdWaterVolumeMeter2M3 = 23.0,
            LastMeterSynchronisation = "2024-01-01T00:00:00Z",
            FwVer = "1.0.0",
            DewPointOffinitDegC = 10.0,
            DehumidificationEquipment = 0,
            CommunicationActiveRelayModule = true,
            CommunicationActiveThermostats = true,
            OperationMode = 0,
            LastStateUpdate = DateTimeOffset.UtcNow,
            Thermostats = new List<SbmApartmentInfoResponse.Thermostat>()
        };

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingAccessRights);
        _sbmServiceMock.Setup(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentList);
        _sbmServiceMock.Setup(x => x.GetApartmentInfo(456, jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentInfo);

        // Act - First call
        await sut.GetApartmentInfo();

        // Act - Second call
        await sut.GetApartmentInfo();

        // Assert - Verify caching works - building access rights and apartment list should only be called once
        _sbmServiceMock.Verify(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
        _sbmServiceMock.Verify(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
        // But apartment info should be called twice (not cached)
        _sbmServiceMock.Verify(x => x.GetApartmentInfo(456, jwtToken.AccessToken, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetApartmentInfo_NoBuildingAccessRights_ThrowsSbmException()
    {
        // Arrange
        var sut = CreateSut();
        var jwtToken = CreateTestToken();
        var emptyBuildingAccessRights = Array.Empty<SbmGetBuildingAccessRightsResponse>();

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyBuildingAccessRights);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SbmException>(() => sut.GetApartmentInfo());
        Assert.Equal("No building access rights found for the user.", exception.Message);
    }

    [Fact]
    public async Task GetApartmentInfo_NoApartmentFound_ThrowsSbmException()
    {
        // Arrange
        var sut = CreateSut();
        var jwtToken = CreateTestToken();
        var buildingAccessRights = new[]
        {
            new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = 123,
                Country = "TestCountry",
                PostalCode = 12345,
                City = "TestCity",
                Street = "TestStreet",
                Temperature = 20.5,
                Humidity = 65.0,
                Name = "TestBuilding"
            }
        };
        var emptyApartmentList = Array.Empty<SbmApartmentListResponse>();

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingAccessRights);
        _sbmServiceMock.Setup(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyApartmentList);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SbmException>(() => sut.GetApartmentInfo());
        Assert.Equal("No apartment found for the user.", exception.Message);
    }

    [Fact]
    public async Task GetApartmentInfo_WithCancellationToken_PassesTokenCorrectly()
    {
        var sut = CreateSut();
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var jwtToken = CreateTestToken();
        var buildingAccessRights = new[]
        {
            new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = 123,
                Country = "TestCountry",
                PostalCode = 12345,
                City = "TestCity",
                Street = "TestStreet",
                Temperature = 20.5,
                Humidity = 65.0,
                Name = "TestBuilding"
            }
        };
        var apartmentList = new[]
        {
            new SbmApartmentListResponse
            {
                BuildingId = 123,
                ApartmentId = 456,
                Name = "TestApartment",
                IsOnline = true
            }
        };
        var apartmentInfo = new SbmApartmentInfoResponse
        {
            Name = "TestApartment",
            ForwardWaterTemperature1DegC = 25.0,
            ReturningWaterTemperature1DegC = 23.0,
            FlowRateLiterPerHour1 = 100.0,
            ForwardWaterTemperature2DegC = 24.0,
            ReturningWaterTemperature2DegC = 22.0,
            FlowRateLiterPerHour2 = 95.0,
            HeatingHeatQuantity1KWh = 150.0,
            CoolingHeatQuantity1KWh = 75.0,
            HeatingCoolingVolumeMeter1M3 = 50.0,
            HeatingHeatQuantity2KWh = 140.0,
            CoolingHeatQuantity2KWh = 70.0,
            HeatingCoolingVolumeMeter2M3 = 45.0,
            HotWaterHeatingQuantity1KWh = 200.0,
            HotWaterVolumeMeter1M3 = 30.0,
            HotWaterHeatingQuantity2KWh = 190.0,
            HotWaterVolumeMeter2M3 = 28.0,
            ColdWaterVolumeMeter1M3 = 25.0,
            ColdWaterVolumeMeter2M3 = 23.0,
            LastMeterSynchronisation = "2024-01-01T00:00:00Z",
            FwVer = "1.0.0",
            DewPointOffinitDegC = 10.0,
            DehumidificationEquipment = 0,
            CommunicationActiveRelayModule = true,
            CommunicationActiveThermostats = true,
            OperationMode = 0,
            LastStateUpdate = DateTimeOffset.UtcNow,
            Thermostats = new List<SbmApartmentInfoResponse.Thermostat>()
        };

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingAccessRights);
        _sbmServiceMock.Setup(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentList);
        _sbmServiceMock.Setup(x => x.GetApartmentInfo(456, jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentInfo);

        // Act
        await sut.GetApartmentInfo(cancellationToken);

        // Assert
        _tokenServiceMock.Verify(x => x.GetToken(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeTemperature_CallsCorrectMethods()
    {
        var sut = CreateSut();
        // Arrange
        var thermostatId = 789;
        var temperature = 24.5;
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var jwtToken = CreateTestToken();
        var changeTemperatureResponse = new SbmChangeTemperatureResponse
        {
            Message = "Temperature changed successfully",
            ThermostatId = thermostatId
        };

        _tokenServiceMock.Setup(x => x.GetToken(cancellationToken))
            .ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x =>
                x.ChangeTemperature(thermostatId, temperature, jwtToken.AccessToken, cancellationToken))
            .ReturnsAsync(changeTemperatureResponse);

        // Act
        await sut.ChangeTemperature(thermostatId, temperature, cancellationToken);

        // Assert
        _tokenServiceMock.Verify(x => x.GetToken(cancellationToken), Times.Once);
        _sbmServiceMock.Verify(
            x => x.ChangeTemperature(thermostatId, temperature, jwtToken.AccessToken, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ChangeTemperature_WithDefaultCancellationToken_CallsCorrectMethods()
    {
        // Arrange
        var sut = CreateSut();
        var thermostatId = 789;
        var temperature = 24.5;
        var jwtToken = CreateTestToken();
        var changeTemperatureResponse = new SbmChangeTemperatureResponse
        {
            Message = "Temperature changed successfully",
            ThermostatId = thermostatId
        };

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x =>
                x.ChangeTemperature(thermostatId, temperature, jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(changeTemperatureResponse);

        // Act
        await sut.ChangeTemperature(thermostatId, temperature);

        // Assert
        _tokenServiceMock.Verify(x => x.GetToken(It.IsAny<CancellationToken>()), Times.Once);
        _sbmServiceMock.Verify(
            x => x.ChangeTemperature(thermostatId, temperature, jwtToken.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0, "heating")]
    [InlineData(1, "cooling")]
    [InlineData(99, "unknown")]
    public async Task GetApartmentInfo_OperationModes_MapsCorrectly(int operationMode, string expectedSystemMode)
    {
        // Arrange
        var sut = CreateSut();
        var jwtToken = CreateTestToken();
        var buildingAccessRights = new[]
        {
            new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = 123,
                Country = "TestCountry",
                PostalCode = 12345,
                City = "TestCity",
                Street = "TestStreet",
                Temperature = 20.5,
                Humidity = 65.0,
                Name = "TestBuilding"
            }
        };
        var apartmentList = new[]
        {
            new SbmApartmentListResponse
            {
                BuildingId = 123,
                ApartmentId = 456,
                Name = "TestApartment",
                IsOnline = true
            }
        };
        var apartmentInfo = new SbmApartmentInfoResponse
        {
            Name = "TestApartment",
            ForwardWaterTemperature1DegC = 25.0,
            ReturningWaterTemperature1DegC = 23.0,
            FlowRateLiterPerHour1 = 100.0,
            ForwardWaterTemperature2DegC = 24.0,
            ReturningWaterTemperature2DegC = 22.0,
            FlowRateLiterPerHour2 = 95.0,
            HeatingHeatQuantity1KWh = 150.0,
            CoolingHeatQuantity1KWh = 75.0,
            HeatingCoolingVolumeMeter1M3 = 50.0,
            HeatingHeatQuantity2KWh = 140.0,
            CoolingHeatQuantity2KWh = 70.0,
            HeatingCoolingVolumeMeter2M3 = 45.0,
            HotWaterHeatingQuantity1KWh = 200.0,
            HotWaterVolumeMeter1M3 = 30.0,
            HotWaterHeatingQuantity2KWh = 190.0,
            HotWaterVolumeMeter2M3 = 28.0,
            ColdWaterVolumeMeter1M3 = 25.0,
            ColdWaterVolumeMeter2M3 = 23.0,
            LastMeterSynchronisation = "2024-01-01T00:00:00Z",
            FwVer = "1.0.0",
            DewPointOffinitDegC = 10.0,
            DehumidificationEquipment = 0,
            CommunicationActiveRelayModule = true,
            CommunicationActiveThermostats = true,
            OperationMode = operationMode,
            LastStateUpdate = DateTimeOffset.UtcNow,
            Thermostats = new List<SbmApartmentInfoResponse.Thermostat>()
        };

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingAccessRights);
        _sbmServiceMock.Setup(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentList);
        _sbmServiceMock.Setup(x => x.GetApartmentInfo(456, jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentInfo);

        // Act
        var result = await sut.GetApartmentInfo();

        // Assert
        Assert.Equal(expectedSystemMode, result.SystemMode);
    }

    private static SbmTokenResponse CreateTestToken()
    {
        return new SbmTokenResponse
        {
            AccessToken = "test-jwt-token-123",
            Expiration = DateTimeOffset.UtcNow.AddHours(1),
            RefreshToken = "refresh-token-123",
            RefreshTokenExpiration = DateTimeOffset.UtcNow.AddDays(7),
            Rights = new List<string> { "read", "write" }
        };
    }

    [Fact]
    public void Dispose_DisposesWithoutException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var exception = Record.Exception(() => sut.Dispose());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Dispose();
        var exception = Record.Exception(() => sut.Dispose());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Dispose_AfterUsage_DisposesSuccessfully()
    {
        // Arrange
        var jwtToken = CreateTestToken();
        var buildingAccessRights = new[]
        {
            new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = 1, Country = "C", PostalCode = 1, City = "C",
                Street = "S", Temperature = 20, Humidity = 50, Name = "B"
            }
        };
        var apartmentList = new[]
        {
            new SbmApartmentListResponse
            {
                BuildingId = 1, ApartmentId = 1, Name = "A", IsOnline = true
            }
        };
        var apartmentInfo = new SbmApartmentInfoResponse
        {
            Name = "A", ForwardWaterTemperature1DegC = 0, ReturningWaterTemperature1DegC = 0,
            FlowRateLiterPerHour1 = 0, ForwardWaterTemperature2DegC = 0, ReturningWaterTemperature2DegC = 0,
            FlowRateLiterPerHour2 = 0, HeatingHeatQuantity1KWh = 0, CoolingHeatQuantity1KWh = 0,
            HeatingCoolingVolumeMeter1M3 = 0, HeatingHeatQuantity2KWh = 0, CoolingHeatQuantity2KWh = 0,
            HeatingCoolingVolumeMeter2M3 = 0, HotWaterHeatingQuantity1KWh = 0, HotWaterVolumeMeter1M3 = 0,
            HotWaterHeatingQuantity2KWh = 0, HotWaterVolumeMeter2M3 = 0, ColdWaterVolumeMeter1M3 = 0,
            ColdWaterVolumeMeter2M3 = 0, LastMeterSynchronisation = "", FwVer = "", DewPointOffinitDegC = 0,
            DehumidificationEquipment = 0, CommunicationActiveRelayModule = true,
            CommunicationActiveThermostats = true, OperationMode = 0, LastStateUpdate = DateTimeOffset.UtcNow,
            Thermostats = new List<SbmApartmentInfoResponse.Thermostat>()
        };

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>())).ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingAccessRights);
        _sbmServiceMock.Setup(x => x.GetApartmentList("1", jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentList);
        _sbmServiceMock.Setup(x => x.GetApartmentInfo(1, jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentInfo);

        var sut = CreateSut();
        await sut.GetApartmentInfo();

        // Act
        var exception = Record.Exception(() => sut.Dispose());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void ImplementsIDisposable()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.IsAssignableFrom<IDisposable>(sut);
        sut.Dispose();
    }

    [Fact]
    public async Task GetApartmentInfo_WithOutdoorWeatherEnabled_PopulatesOutdoorFields()
    {
        // Arrange
        var sut = CreateSut(outdoorWeatherEnabled: true);
        var jwtToken = CreateTestToken();
        var buildingAccessRights = new[]
        {
            new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = 123, Country = "TC", PostalCode = 1, City = "TC",
                Street = "TS", Temperature = 12.5, Humidity = 68.0, Name = "TB"
            }
        };
        var apartmentList = new[]
        {
            new SbmApartmentListResponse
                { BuildingId = 123, ApartmentId = 456, Name = "TA", IsOnline = true }
        };
        var apartmentInfo = CreateMinimalApartmentInfo();

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>())).ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingAccessRights);
        _sbmServiceMock.Setup(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentList);
        _sbmServiceMock.Setup(x => x.GetApartmentInfo(456, jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentInfo);

        // Act
        var result = await sut.GetApartmentInfo();

        // Assert
        Assert.Equal(12.5, result.OutdoorTemperature);
        Assert.Equal(68.0, result.OutdoorHumidity);
    }

    [Fact]
    public async Task GetApartmentInfo_WithOutdoorWeatherEnabled_CallsBuildingAccessRightsOnEveryInvocation()
    {
        // Arrange
        var sut = CreateSut(outdoorWeatherEnabled: true);
        var jwtToken = CreateTestToken();
        var buildingAccessRights = new[]
        {
            new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = 123, Country = "TC", PostalCode = 1, City = "TC",
                Street = "TS", Temperature = 12.5, Humidity = 68.0, Name = "TB"
            }
        };
        var apartmentList = new[]
        {
            new SbmApartmentListResponse
                { BuildingId = 123, ApartmentId = 456, Name = "TA", IsOnline = true }
        };
        var apartmentInfo = CreateMinimalApartmentInfo();

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>())).ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingAccessRights);
        _sbmServiceMock.Setup(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentList);
        _sbmServiceMock.Setup(x => x.GetApartmentInfo(456, jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentInfo);

        // Act
        await sut.GetApartmentInfo();
        await sut.GetApartmentInfo();

        // Assert: building access rights called twice (not cached)
        _sbmServiceMock.Verify(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        // Apartment list still only called once (apartment ID is cached)
        _sbmServiceMock.Verify(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetApartmentInfo_WithOutdoorWeatherDisabled_OutdoorFieldsAreNull()
    {
        // Arrange
        var sut = CreateSut(outdoorWeatherEnabled: false);
        var jwtToken = CreateTestToken();
        var buildingAccessRights = new[]
        {
            new SbmGetBuildingAccessRightsResponse
            {
                BuildingId = 123, Country = "TC", PostalCode = 1, City = "TC",
                Street = "TS", Temperature = 12.5, Humidity = 68.0, Name = "TB"
            }
        };
        var apartmentList = new[]
        {
            new SbmApartmentListResponse
                { BuildingId = 123, ApartmentId = 456, Name = "TA", IsOnline = true }
        };
        var apartmentInfo = CreateMinimalApartmentInfo();

        _tokenServiceMock.Setup(x => x.GetToken(It.IsAny<CancellationToken>())).ReturnsAsync(jwtToken);
        _sbmServiceMock.Setup(x => x.GetBuildingAccessRights(jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildingAccessRights);
        _sbmServiceMock.Setup(x => x.GetApartmentList("123", jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentList);
        _sbmServiceMock.Setup(x => x.GetApartmentInfo(456, jwtToken.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartmentInfo);

        // Act
        var result = await sut.GetApartmentInfo();

        // Assert
        Assert.Null(result.OutdoorTemperature);
        Assert.Null(result.OutdoorHumidity);
    }

    private static SbmApartmentInfoResponse CreateMinimalApartmentInfo() =>
        new()
        {
            Name = "TA", ForwardWaterTemperature1DegC = 0, ReturningWaterTemperature1DegC = 0,
            FlowRateLiterPerHour1 = 0, ForwardWaterTemperature2DegC = 0, ReturningWaterTemperature2DegC = 0,
            FlowRateLiterPerHour2 = 0, HeatingHeatQuantity1KWh = 0, CoolingHeatQuantity1KWh = 0,
            HeatingCoolingVolumeMeter1M3 = 0, HeatingHeatQuantity2KWh = 0, CoolingHeatQuantity2KWh = 0,
            HeatingCoolingVolumeMeter2M3 = 0, HotWaterHeatingQuantity1KWh = 0, HotWaterVolumeMeter1M3 = 0,
            HotWaterHeatingQuantity2KWh = 0, HotWaterVolumeMeter2M3 = 0, ColdWaterVolumeMeter1M3 = 0,
            ColdWaterVolumeMeter2M3 = 0, LastMeterSynchronisation = "", FwVer = "", DewPointOffinitDegC = 0,
            DehumidificationEquipment = 0, CommunicationActiveRelayModule = true,
            CommunicationActiveThermostats = true, OperationMode = 0, LastStateUpdate = DateTimeOffset.UtcNow,
            Thermostats = new List<SbmApartmentInfoResponse.Thermostat>()
        };
}