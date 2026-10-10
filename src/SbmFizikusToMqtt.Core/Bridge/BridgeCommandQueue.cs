using System.Threading.Channels;

namespace SbmFizikusToMqtt.Core.Bridge;

/// <summary>
///     The single queue feeding <see cref="BridgeProcessor" />. Producers (poll timer, MQTT adapter) enqueue from any
///     thread; one consumer processes commands in order. At most one <see cref="PollRequested" /> is pending at a
///     time, so polls cannot pile up while SBM is slow.
/// </summary>
public sealed class BridgeCommandQueue
{
    private readonly Channel<BridgeCommand> _channel =
        Channel.CreateUnbounded<BridgeCommand>(new UnboundedChannelOptions { SingleReader = true });

    private int _pollPending;

    public void Enqueue(BridgeCommand command)
    {
        if (command is PollRequested && Interlocked.Exchange(ref _pollPending, 1) == 1)
            return;

        _channel.Writer.TryWrite(command);
    }

    public async IAsyncEnumerable<BridgeCommand> ReadAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        await foreach (var command in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (command is PollRequested)
                Interlocked.Exchange(ref _pollPending, 0);

            yield return command;
        }
    }
}