using System.Diagnostics;
using System.Net;
using SbmFizikusToMqtt.Infrastructure.Sbm;
using SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

namespace SbmFizikusToMqtt.Infrastructure.Tests.Sbm;

public sealed class SbmTelemetryTests
{
    private const string Token = "jwt";
    private readonly SbmTestHost _host = new();

    [Fact]
    public async Task SendAsync_EveryOperation_CreatesSpanNamedAfterOperation()
    {
        // Arrange
        var sut = _host.Client;

        // Act
        var spans = await SpanCapture.CaptureAsync(async () =>
        {
            await sut.LoginAsync("user@example.com", "secret", CancellationToken.None);
            await sut.GetBuildingAccessRightsAsync(Token, CancellationToken.None);
            await sut.GetApartmentAccessRightsAsync(24, Token, CancellationToken.None);
            await sut.GetApartmentDataAsync(702, Token, CancellationToken.None);
            await sut.UpdateThermostatSettingsAsync(1763, 21.5, Token, CancellationToken.None);
        }, SbmTelemetry.ActivitySourceName);

        // Assert
        Assert.Equal(
        [
            "SBM get_jw_token", "SBM get_building_access_rights", "SBM get_apartment_access_rights",
            "SBM get_apartment_data", "SBM update_thermostat_settings"
        ], spans.Select(x => x.DisplayName));
        Assert.All(spans, span =>
        {
            Assert.Equal(span.DisplayName["SBM ".Length..], span.GetTagItem("sbm.operation"));
            Assert.Equal(ActivityStatusCode.Unset, span.Status);
        });
    }

    [Fact]
    public async Task SendAsync_ErrorStatus_MarksSpanAsFailed()
    {
        // Arrange
        _host.Handler.Respond("get_apartment_data", HttpStatusCode.InternalServerError, "boom");

        // Act
        var spans = await SpanCapture.CaptureAsync(() =>
            _host.Client.GetApartmentDataAsync(702, Token, CancellationToken.None), SbmTelemetry.ActivitySourceName);

        // Assert
        var span = Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Contains("500", span.StatusDescription);
        Assert.Contains(span.Events, x => x.Name == "exception");
    }

    [Fact]
    public async Task SendAsync_EmptyBuildingList_MarksSpanAsFailed()
    {
        // Arrange
        _host.Handler.Respond("get_building_access_rights", HttpStatusCode.OK, "[]");

        // Act
        var spans = await SpanCapture.CaptureAsync(() =>
            _host.Client.GetBuildingAccessRightsAsync(Token, CancellationToken.None), SbmTelemetry.ActivitySourceName);

        // Assert
        var span = Assert.Single(spans);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        Assert.Equal("SBM returned no buildings for the account", span.StatusDescription);
    }
}