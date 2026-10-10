using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using SbmFizikusToMqtt.Infrastructure.Sbm;
using SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

namespace SbmFizikusToMqtt.Infrastructure.Tests.Sbm;

public sealed class SbmSessionTests
{
    private static readonly JsonSerializerOptions SnapshotJson = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GetApartmentAsync_RealFixtures_MapsDomainWithUtcTimestamps()
    {
        // Arrange
        var sut = new SbmTestHost().Session;

        // Act
        var apartment = await sut.GetApartmentAsync(CancellationToken.None);

        // Assert
        Snapshot.Match(JsonSerializer.Serialize(apartment, SnapshotJson));
    }

    [Fact]
    public async Task GetApartmentAsync_OutdoorWeatherDisabled_ResolvesApartmentOnce()
    {
        // Arrange
        var host = new SbmTestHost(outdoorWeatherEnabled: false);

        // Act
        await host.Session.GetApartmentAsync(CancellationToken.None);
        var apartment = await host.Session.GetApartmentAsync(CancellationToken.None);

        // Assert
        Assert.Equal(
            ["get_jw_token", "get_building_access_rights", "get_apartment_access_rights", "get_apartment_data", "get_apartment_data"],
            host.Handler.Operations);
        Assert.Null(apartment.OutdoorTemperature);
        Assert.Null(apartment.OutdoorHumidity);
    }

    [Fact]
    public async Task GetApartmentAsync_OutdoorWeatherEnabled_FetchesBuildingsOnEveryPoll()
    {
        // Arrange
        var host = new SbmTestHost(outdoorWeatherEnabled: true);

        // Act
        await host.Session.GetApartmentAsync(CancellationToken.None);
        var apartment = await host.Session.GetApartmentAsync(CancellationToken.None);

        // Assert
        Assert.Equal(
        [
            "get_jw_token", "get_building_access_rights", "get_apartment_access_rights", "get_apartment_data",
            "get_building_access_rights", "get_apartment_data"
        ], host.Handler.Operations);
        Assert.Equal(19.9, apartment.OutdoorTemperature);
        Assert.Equal(49.9, apartment.OutdoorHumidity);
    }

    [Fact]
    public async Task GetApartmentAsync_TokenWithinRefreshMargin_LogsInAgain()
    {
        // Arrange
        var host = new SbmTestHost(outdoorWeatherEnabled: false);
        var expiration = host.TimeProvider.GetUtcNow().AddMinutes(10);
        host.Handler.Respond("get_jw_token", HttpStatusCode.OK,
            $"{{\"token\": \"short-lived\", \"expiration\": \"{expiration.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}+00:00\"}}");
        await host.Session.GetApartmentAsync(CancellationToken.None);

        // Act
        host.TimeProvider.Advance(TimeSpan.FromMinutes(9) + TimeSpan.FromSeconds(31));
        await host.Session.GetApartmentAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, host.Handler.Operations.Count(x => x == "get_jw_token"));
    }

    [Fact]
    public async Task GetApartmentAsync_ValidToken_ReusesIt()
    {
        // Arrange
        var host = new SbmTestHost(outdoorWeatherEnabled: false);

        // Act
        await host.Session.GetApartmentAsync(CancellationToken.None);
        host.TimeProvider.Advance(TimeSpan.FromHours(12));
        await host.Session.GetApartmentAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, host.Handler.Operations.Count(x => x == "get_jw_token"));
    }

    [Fact]
    public async Task GetApartmentAsync_TokenRejected_LogsInAgainAndRetriesOnce()
    {
        // Arrange
        var host = new SbmTestHost(outdoorWeatherEnabled: false);
        await host.Session.GetApartmentAsync(CancellationToken.None);
        host.Handler.Requests.Clear();
        host.Handler.Respond("get_apartment_data", HttpStatusCode.Unauthorized, "{\"message\":\"Invalid token\"}");

        // Act
        var apartment = await host.Session.GetApartmentAsync(CancellationToken.None);

        // Assert
        Assert.Equal(4, apartment.Thermostats.Count);
        Assert.Equal(
        [
            "get_apartment_data", "get_jw_token", "get_building_access_rights", "get_apartment_access_rights",
            "get_apartment_data"
        ], host.Handler.Operations);
    }

    [Fact]
    public async Task GetApartmentAsync_TokenRejectedTwice_Throws()
    {
        // Arrange
        var host = new SbmTestHost(outdoorWeatherEnabled: false);
        host.Handler
            .Respond("get_apartment_data", HttpStatusCode.Unauthorized, "no")
            .Respond("get_apartment_data", HttpStatusCode.Unauthorized, "no");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SbmApiException>(() =>
            host.Session.GetApartmentAsync(CancellationToken.None));
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task GetApartmentAsync_ServerError_ThrowsWithoutLoggingInAgain()
    {
        // Arrange
        var host = new SbmTestHost(outdoorWeatherEnabled: false);
        host.Handler.Respond("get_apartment_data", HttpStatusCode.InternalServerError, "oops");

        // Act
        await Assert.ThrowsAsync<SbmApiException>(() => host.Session.GetApartmentAsync(CancellationToken.None));

        // Assert
        Assert.Equal(1, host.Handler.Operations.Count(x => x == "get_jw_token"));
    }

    [Fact]
    public async Task SetTargetTemperatureAsync_ValidRequest_SendsSetpointWithToken()
    {
        // Arrange
        var host = new SbmTestHost();

        // Act
        await host.Session.SetTargetTemperatureAsync(1763, 21.5, CancellationToken.None);

        // Assert
        var request = Assert.Single(host.Handler.Requests, x => x.Operation == "update_thermostat_settings");
        Assert.Contains("\"thermostat_id\":1763", request.Body);
        Assert.Contains("\"temp_setpoint_degC\":21.5", request.Body);
    }
}