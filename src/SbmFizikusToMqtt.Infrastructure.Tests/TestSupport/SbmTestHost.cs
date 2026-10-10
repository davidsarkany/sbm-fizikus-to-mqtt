using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SbmFizikusToMqtt.Infrastructure.Sbm;

namespace SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

/// <summary>Wires a real <see cref="SbmClient" /> and <see cref="SbmSession" /> to <see cref="SbmStubHandler" />.</summary>
internal sealed class SbmTestHost
{
    public SbmTestHost(bool outdoorWeatherEnabled = true)
    {
        Client = new SbmClient(new StubHttpClientFactory(Handler));
        Session = new SbmSession(Client,
            Microsoft.Extensions.Options.Options.Create(new SbmOptions
            {
                Username = "user@example.com",
                Password = "secret",
                BaseUrl = "https://sbm.example.com",
                OutdoorWeatherEnabled = outdoorWeatherEnabled
            }),
            TimeProvider,
            NullLogger<SbmSession>.Instance);
    }

    public SbmStubHandler Handler { get; } = new();

    /// <summary>Starts just after the fixture data was captured; the fixture token is valid until 2099.</summary>
    public FakeTimeProvider TimeProvider { get; } = new(new DateTimeOffset(2026, 10, 10, 12, 46, 0, TimeSpan.Zero));

    public SbmClient Client { get; }

    public SbmSession Session { get; }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(SbmClient.HttpClientName, name);
            return new HttpClient(handler, false) { BaseAddress = new Uri("https://sbm.example.com") };
        }
    }
}