using Microsoft.Extensions.Logging.Abstractions;
using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Publishing;
using SbmFizikusToMqtt.Infrastructure.Mqtt;

namespace SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

/// <summary>A <see cref="MqttGateway" /> wired to a broker, recording everything it enqueues.</summary>
internal sealed class GatewayFixture : IAsyncDisposable
{
    private readonly CancellationTokenSource _readerCts = new();
    private readonly Task _reader;

    public GatewayFixture(InProcessBroker broker)
    {
        Status = new BridgeStatus(TimeProvider.System);
        Gateway = new MqttGateway(
            Microsoft.Extensions.Options.Options.Create(new MqttOptions
            {
                Host = "127.0.0.1",
                Port = broker.Port,
                ClientId = "gateway-under-test"
            }),
            Topics, Status, Queue, TimeProvider.System, NullLogger<MqttGateway>.Instance);

        _reader = Task.Run(async () =>
        {
            try
            {
                await foreach (var command in Queue.ReadAllAsync(_readerCts.Token))
                    lock (Commands)
                        Commands.Add(command);
            }
            catch (OperationCanceledException)
            {
                // Expected on dispose.
            }
        });
    }

    public static MqttTopics Topics { get; } = new(new TopicOptions
    {
        SbmTopic = "sbm_fizikus",
        HomeAssistantTopic = "homeassistant"
    });

    public BridgeCommandQueue Queue { get; } = new();

    public BridgeStatus Status { get; }

    public MqttGateway Gateway { get; }

    public List<BridgeCommand> Commands { get; } = [];

    public int ConnectedCount
    {
        get
        {
            lock (Commands)
                return Commands.Count(x => x is MqttConnected);
        }
    }

    public IReadOnlyList<MqttMessageReceived> ReceivedMessages()
    {
        lock (Commands)
            return Commands.OfType<MqttMessageReceived>().ToList();
    }

    public async ValueTask DisposeAsync()
    {
        await Gateway.StopAsync(CancellationToken.None);
        Gateway.Dispose();
        await _readerCts.CancelAsync();
        await _reader;
        _readerCts.Dispose();
    }
}