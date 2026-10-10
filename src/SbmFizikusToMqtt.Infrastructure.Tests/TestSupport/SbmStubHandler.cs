using System.Net;
using System.Text;
using System.Text.Json;

namespace SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

/// <summary>
///     Fake SBM API. Each request is answered with the anonymized real fixture named after its <c>operation</c>,
///     unless a one-shot override was queued with <see cref="Respond" />.
/// </summary>
internal sealed class SbmStubHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Queue<(HttpStatusCode Status, string Body)>> _overrides = [];

    public List<RecordedRequest> Requests { get; } = [];

    public IEnumerable<string> Operations => Requests.Select(x => x.Operation);

    public SbmStubHandler Respond(string operation, HttpStatusCode status, string body)
    {
        if (!_overrides.TryGetValue(operation, out var queue))
            _overrides[operation] = queue = new Queue<(HttpStatusCode, string)>();

        queue.Enqueue((status, body));
        return this;
    }

    public static string Fixture(string operation)
    {
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "sbm", $"{operation}.json"));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = await request.Content!.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        var operation = document.RootElement.GetProperty("operation").GetString()!;
        Requests.Add(new RecordedRequest(request.Method.Method, request.RequestUri!.ToString(), operation, body,
            request.Content.Headers.ContentType?.ToString()));

        var (status, responseBody) = _overrides.TryGetValue(operation, out var queue) && queue.Count > 0
            ? queue.Dequeue()
            : (HttpStatusCode.OK, Fixture(operation));

        return new HttpResponseMessage(status)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
    }

    internal sealed record RecordedRequest(
        string Method,
        string Uri,
        string Operation,
        string Body,
        string? ContentType);
}