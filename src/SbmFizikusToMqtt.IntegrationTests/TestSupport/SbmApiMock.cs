using System.Text.Json;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace SbmFizikusToMqtt.IntegrationTests.TestSupport;

/// <summary>Fake SBM API answering every operation with the anonymized real response fixture.</summary>
public sealed class SbmApiMock : IDisposable
{
    private static readonly string[] Operations =
    [
        "get_jw_token",
        "get_building_access_rights",
        "get_apartment_access_rights",
        "get_apartment_data",
        "update_thermostat_settings"
    ];

    private readonly WireMockServer _server = WireMockServer.Start();

    public SbmApiMock()
    {
        foreach (var operation in Operations)
        {
            var fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "sbm",
                $"{operation}.json"));
            _server
                .Given(Request.Create().WithPath("/frontend").UsingPut()
                    .WithBody(new JsonPartialMatcher($"{{\"operation\": \"{operation}\"}}")))
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(fixture));
        }
    }

    public string Url => _server.Url!;

    /// <summary>Payloads of all received requests for the given operation.</summary>
    public IReadOnlyList<JsonElement> Requests(string operation)
    {
        return _server.LogEntries
            .Select(x => JsonDocument.Parse(x.RequestMessage?.Body ?? "{}").RootElement)
            .Where(x => x.TryGetProperty("operation", out var op) && op.GetString() == operation)
            .Select(x => x.GetProperty("payload"))
            .ToList();
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}