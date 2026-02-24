using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using SbmFizikusToMqtt.SbmConnector.Exceptions;
using SbmFizikusToMqtt.SbmConnector.Models.Response;
using SbmFizikusToMqtt.SbmConnector.Services;

namespace SbmFizikusToMqtt.SbmConnector.Tests.Services;

public class SbmServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly SbmService _sbmService;

    public SbmServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://test.api.com")
        };

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock
            .Setup(x => x.CreateClient("SbmClient"))
            .Returns(httpClient);

        _sbmService = new SbmService(_httpClientFactoryMock.Object);
    }

    #region GetToken Tests

    [Fact]
    public async Task GetToken_ValidCredentials_ReturnsTokenResponse()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";
        var expectedResponse = new SbmTokenResponse
        {
            AccessToken = "test-token",
            Expiration = DateTimeOffset.UtcNow.AddHours(1),
            RefreshToken = "refresh-token",
            RefreshTokenExpiration = DateTimeOffset.UtcNow.AddDays(7),
            Rights = new List<string> { "read", "write" }
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sbmService.GetToken(username, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.AccessToken, result.AccessToken);
        Assert.Equal(expectedResponse.RefreshToken, result.RefreshToken);
        Assert.Equal(expectedResponse.Rights.Count(), result.Rights.Count());
        VerifyHttpRequest(HttpMethod.Put, "/frontend", Times.Once());
    }

    [Fact]
    public async Task GetToken_InvalidJson_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";
        SetupHttpResponse(HttpStatusCode.OK, "invalid json");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<SbmInvalidResponseException>(() => _sbmService.GetToken(username, password));
        Assert.Contains("login failed", exception.Message);
    }

    [Fact]
    public async Task GetToken_NullResponse_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";
        SetupHttpResponse(HttpStatusCode.OK, "null");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<SbmInvalidResponseException>(() => _sbmService.GetToken(username, password));
        Assert.Contains("login failed", exception.Message);
    }

    [Fact]
    public async Task GetToken_HttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var username = "testuser";
        var password = "testpass";
        SetupHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _sbmService.GetToken(username, password));
    }

    #endregion

    #region GetBuildingAccessRights Tests

    [Fact]
    public async Task GetBuildingAccessRights_ValidToken_ReturnsBuildingList()
    {
        // Arrange
        var token = "test-token";
        // The converter expects an array of arrays, each inner array has 8 elements:
        // [BuildingId, Country, PostalCode, City, Street, Temperature, Humidity, Name]
        var responseJson = @"[
            [1, ""Hungary"", 1234, ""Budapest"", ""Test Street"", 22.5, 45.0, ""Building 1""],
            [2, ""Hungary"", 5678, ""Debrecen"", ""Main Street"", 21.0, 50.0, ""Building 2""]
        ]";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sbmService.GetBuildingAccessRights(token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(1, result[0].BuildingId);
        Assert.Equal("Building 1", result[0].Name);
        Assert.Equal("Hungary", result[0].Country);
        Assert.Equal(1234, result[0].PostalCode);
        Assert.Equal("Budapest", result[0].City);
        Assert.Equal("Test Street", result[0].Street);
        Assert.Equal(22.5, result[0].Temperature);
        Assert.Equal(45.0, result[0].Humidity);
        Assert.Equal(2, result[1].BuildingId);
        Assert.Equal("Building 2", result[1].Name);
        VerifyHttpRequest(HttpMethod.Put, "/frontend", Times.Once());
    }

    [Fact]
    public async Task GetBuildingAccessRights_EmptyResponse_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var token = "test-token";
        SetupHttpResponse(HttpStatusCode.OK, "[]");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<SbmInvalidResponseException>(() => _sbmService.GetBuildingAccessRights(token));
        Assert.Contains("empty building access rights response", exception.Message);
    }

    [Fact]
    public async Task GetBuildingAccessRights_InvalidJson_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var token = "test-token";
        SetupHttpResponse(HttpStatusCode.OK, "invalid json");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<SbmInvalidResponseException>(() => _sbmService.GetBuildingAccessRights(token));
        Assert.Contains("Failed to parse building access rights response", exception.Message);
    }

    #endregion

    #region GetApartmentList Tests

    [Fact]
    public async Task GetApartmentList_ValidParameters_ReturnsApartmentList()
    {
        // Arrange
        var buildingId = "123";
        var token = "test-token";
        // The converter expects an array of arrays, each inner array has 4 elements:
        // [BuildingId, ApartmentId, Name, IsOnline]
        var responseJson = @"[
            [123, 1, ""Apartment 1"", true],
            [123, 2, ""Apartment 2"", false]
        ]";
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sbmService.GetApartmentList(buildingId, token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal(1, result[0].ApartmentId);
        Assert.Equal("Apartment 1", result[0].Name);
        Assert.Equal(123, result[0].BuildingId);
        Assert.True(result[0].IsOnline);
        Assert.Equal(2, result[1].ApartmentId);
        Assert.Equal("Apartment 2", result[1].Name);
        Assert.False(result[1].IsOnline);
        VerifyHttpRequest(HttpMethod.Put, "/frontend", Times.Once());
    }

    [Fact]
    public async Task GetApartmentList_EmptyResponse_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var buildingId = "123";
        var token = "test-token";
        SetupHttpResponse(HttpStatusCode.OK, "[]");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<SbmInvalidResponseException>(() =>
                _sbmService.GetApartmentList(buildingId, token));
        Assert.Contains("empty apartment list", exception.Message);
    }

    [Fact]
    public async Task GetApartmentList_InvalidJson_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var buildingId = "123";
        var token = "test-token";
        SetupHttpResponse(HttpStatusCode.OK, "invalid json");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<SbmInvalidResponseException>(() =>
                _sbmService.GetApartmentList(buildingId, token));
        Assert.Contains("Failed to parse apartment list", exception.Message);
    }

    #endregion

    #region ChangeTemperature Tests

    [Fact]
    public async Task ChangeTemperature_ValidParameters_ReturnsChangeResponse()
    {
        // Arrange
        var thermostatId = 1;
        var temperature = 22.5;
        var token = "test-token";
        var expectedResponse = new SbmChangeTemperatureResponse
        {
            Message = "Temperature changed successfully",
            ThermostatId = thermostatId
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sbmService.ChangeTemperature(thermostatId, temperature, token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Message, result.Message);
        Assert.Equal(expectedResponse.ThermostatId, result.ThermostatId);
        VerifyHttpRequest(HttpMethod.Put, "/frontend", Times.Once());
    }

    [Fact]
    public async Task ChangeTemperature_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var thermostatId = 1;
        var temperature = 22.5;
        var token = "test-token";
        var cancellationToken = new CancellationTokenSource().Token;
        var expectedResponse = new SbmChangeTemperatureResponse
        {
            Message = "Temperature changed",
            ThermostatId = thermostatId
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sbmService.ChangeTemperature(thermostatId, temperature, token, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.ThermostatId, result.ThermostatId);
    }

    [Fact]
    public async Task ChangeTemperature_InvalidJson_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var thermostatId = 1;
        var temperature = 22.5;
        var token = "test-token";
        SetupHttpResponse(HttpStatusCode.OK, "invalid json");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SbmInvalidResponseException>(() =>
            _sbmService.ChangeTemperature(thermostatId, temperature, token));
        Assert.Contains("deserialize failed", exception.Message);
    }

    [Fact]
    public async Task ChangeTemperature_NullResponse_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var thermostatId = 1;
        var temperature = 22.5;
        var token = "test-token";
        SetupHttpResponse(HttpStatusCode.OK, "null");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SbmInvalidResponseException>(() =>
            _sbmService.ChangeTemperature(thermostatId, temperature, token));
        Assert.Contains("deserialize failed", exception.Message);
    }

    #endregion

    #region GetApartmentInfo Tests

    [Fact]
    public async Task GetApartmentInfo_ValidParameters_ReturnsApartmentInfo()
    {
        // Arrange
        var apartmentId = 1;
        var token = "test-token";
        var expectedResponse = new SbmApartmentInfoResponse
        {
            Name = "Test Apartment",
            ForwardWaterTemperature1DegC = 25.5,
            ReturningWaterTemperature1DegC = 20.0,
            FlowRateLiterPerHour1 = 100.0,
            ForwardWaterTemperature2DegC = 26.0,
            ReturningWaterTemperature2DegC = 21.0,
            FlowRateLiterPerHour2 = 110.0,
            HeatingHeatQuantity1KWh = 50.0,
            CoolingHeatQuantity1KWh = 30.0,
            HeatingCoolingVolumeMeter1M3 = 10.0,
            HeatingHeatQuantity2KWh = 55.0,
            CoolingHeatQuantity2KWh = 35.0,
            HeatingCoolingVolumeMeter2M3 = 12.0,
            HotWaterHeatingQuantity1KWh = 40.0,
            HotWaterVolumeMeter1M3 = 8.0,
            HotWaterHeatingQuantity2KWh = 45.0,
            HotWaterVolumeMeter2M3 = 9.0,
            ColdWaterVolumeMeter1M3 = 15.0,
            ColdWaterVolumeMeter2M3 = 16.0,
            LastMeterSynchronisation = "2026-02-15T10:00:00Z",
            LastStateUpdate = DateTimeOffset.UtcNow,
            FwVer = "1.2.3",
            OperationMode = 1,
            DewPointOffinitDegC = 10.5,
            DehumidificationEquipment = 0,
            Thermostats = new List<SbmApartmentInfoResponse.Thermostat>
            {
                new()
                {
                    Id = 1,
                    ThermostatNo = 1,
                    Name = "Living Room",
                    ConfigUpdatedByWebapp = false,
                    TemperatureSetpointDegC = 22.0,
                    CondensationRiskLevel = 0.5,
                    Active = true,
                    MeasuredTempDegC = 21.5,
                    MeasuredHumPerc = 45.0,
                    DewPointDegC = 10.0,
                    LastStateUpdate = DateTimeOffset.UtcNow
                }
            },
            CommunicationActiveRelayModule = true,
            CommunicationActiveThermostats = true
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _sbmService.GetApartmentInfo(apartmentId, token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Name, result.Name);
        Assert.Equal(expectedResponse.ForwardWaterTemperature1DegC, result.ForwardWaterTemperature1DegC);
        Assert.Equal(expectedResponse.FwVer, result.FwVer);
        Assert.Single(result.Thermostats);
        Assert.Equal("Living Room", result.Thermostats.First().Name);
        Assert.True(result.CommunicationActiveRelayModule);
        VerifyHttpRequest(HttpMethod.Put, "/frontend", Times.Once());
    }

    [Fact]
    public async Task GetApartmentInfo_InvalidJson_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var apartmentId = 1;
        var token = "test-token";
        SetupHttpResponse(HttpStatusCode.OK, "invalid json");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<SbmInvalidResponseException>(() =>
                _sbmService.GetApartmentInfo(apartmentId, token));
        Assert.Contains("get apartment info failed", exception.Message);
    }

    [Fact]
    public async Task GetApartmentInfo_NullResponse_ThrowsSbmInvalidResponseException()
    {
        // Arrange
        var apartmentId = 1;
        var token = "test-token";
        SetupHttpResponse(HttpStatusCode.OK, "null");

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<SbmInvalidResponseException>(() =>
                _sbmService.GetApartmentInfo(apartmentId, token));
        Assert.Contains("get apartment info failed", exception.Message);
    }

    [Fact]
    public async Task GetApartmentInfo_HttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var apartmentId = 1;
        var token = "test-token";
        SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _sbmService.GetApartmentInfo(apartmentId, token));
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void VerifyHttpRequest(HttpMethod method, string requestUri, Times times)
    {
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                times,
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains(requestUri)),
                ItExpr.IsAny<CancellationToken>());
    }

    #endregion
}
