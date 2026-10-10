using System.Net;
using System.Text;
using SbmFizikusToMqtt.Infrastructure.Sbm;
using SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

namespace SbmFizikusToMqtt.Infrastructure.Tests.Sbm;

public sealed class SbmClientTests
{
    private const string Token = "jwt";
    private readonly SbmTestHost _host = new();

    [Fact]
    public async Task AllOperations_RequestBodies_MatchSbmContract()
    {
        // Arrange
        var sut = _host.Client;

        // Act
        await sut.LoginAsync("user@example.com", "secret", CancellationToken.None);
        await sut.GetBuildingAccessRightsAsync(Token, CancellationToken.None);
        await sut.GetApartmentAccessRightsAsync(24, Token, CancellationToken.None);
        await sut.GetApartmentDataAsync(702, Token, CancellationToken.None);
        await sut.UpdateThermostatSettingsAsync(1763, 21.5, Token, CancellationToken.None);

        // Assert
        var snapshot = new StringBuilder();
        foreach (var request in _host.Handler.Requests)
            snapshot.AppendLine($"{request.Method} {request.Uri} [{request.ContentType}]").AppendLine(request.Body);
        Snapshot.Match(snapshot.ToString());
    }

    [Fact]
    public void HashPassword_KnownValue_ReturnsLowercaseSha256Hex()
    {
        // Arrange & Act
        var hash = SbmClient.HashPassword("secret");

        // Assert
        Assert.Equal("2bb80d537b1da3e38bd30361aa855686bde0eacd7162fef6a25fe97bf527a25b", hash);
    }

    [Fact]
    public async Task GetApartmentDataAsync_ErrorStatus_ThrowsApiExceptionWithBody()
    {
        // Arrange
        _host.Handler.Respond("get_apartment_data", HttpStatusCode.Unauthorized, "{\"message\":\"Token expired\"}");

        // Act
        var exception = await Assert.ThrowsAsync<SbmApiException>(() =>
            _host.Client.GetApartmentDataAsync(702, Token, CancellationToken.None));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.True(exception.IsAuthenticationFailure);
        Assert.Contains("Token expired", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.Forbidden, true)]
    public async Task GetApartmentDataAsync_ErrorStatus_ClassifiesAuthenticationFailure(HttpStatusCode status,
        bool expected)
    {
        // Arrange
        _host.Handler.Respond("get_apartment_data", status, "error");

        // Act
        var exception = await Assert.ThrowsAsync<SbmApiException>(() =>
            _host.Client.GetApartmentDataAsync(702, Token, CancellationToken.None));

        // Assert
        Assert.Equal(expected, exception.IsAuthenticationFailure);
    }

    [Fact]
    public async Task GetApartmentDataAsync_InvalidJson_ThrowsInvalidResponseExceptionWithBody()
    {
        // Arrange
        _host.Handler.Respond("get_apartment_data", HttpStatusCode.OK, "{\"operationMode\": 0}");

        // Act
        var exception = await Assert.ThrowsAsync<SbmInvalidResponseException>(() =>
            _host.Client.GetApartmentDataAsync(702, Token, CancellationToken.None));

        // Assert
        Assert.Contains("{\"operationMode\": 0}", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InvalidResponse_RedactsBodyBecauseItMayContainTokens()
    {
        // Arrange
        _host.Handler.Respond("get_jw_token", HttpStatusCode.OK, "{\"token\": \"secret-token\"}");

        // Act
        var exception = await Assert.ThrowsAsync<SbmInvalidResponseException>(() =>
            _host.Client.LoginAsync("user", "password", CancellationToken.None));

        // Assert
        Assert.DoesNotContain("secret-token", exception.Message);
    }

    [Theory]
    [InlineData("get_building_access_rights")]
    [InlineData("get_apartment_access_rights")]
    public async Task AccessRights_EmptyList_ThrowsInvalidResponseException(string operation)
    {
        // Arrange
        _host.Handler.Respond(operation, HttpStatusCode.OK, "[]");

        // Act & Assert
        await Assert.ThrowsAsync<SbmInvalidResponseException>(() => operation == "get_building_access_rights"
            ? _host.Client.GetBuildingAccessRightsAsync(Token, CancellationToken.None)
            : _host.Client.GetApartmentAccessRightsAsync(24, Token, CancellationToken.None));
    }
}