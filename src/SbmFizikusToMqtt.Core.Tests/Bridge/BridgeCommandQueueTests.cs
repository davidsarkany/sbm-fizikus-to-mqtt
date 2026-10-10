using SbmFizikusToMqtt.Core.Bridge;

namespace SbmFizikusToMqtt.Core.Tests.Bridge;

public sealed class BridgeCommandQueueTests
{
    private readonly BridgeCommandQueue _sut = new();

    [Fact]
    public async Task Enqueue_MixedCommands_AreReadInOrder()
    {
        // Arrange
        BridgeCommand[] commands =
        [
            new MqttConnected(),
            new MqttMessageReceived("a", "1"),
            new PollRequested(),
            new MqttMessageReceived("b", "2")
        ];

        // Act
        foreach (var command in commands)
            _sut.Enqueue(command);

        // Assert
        Assert.Equal(commands, await ReadAsync(commands.Length));
    }

    [Fact]
    public async Task Enqueue_PollWhilePollPending_IsCoalesced()
    {
        // Arrange & Act
        _sut.Enqueue(new PollRequested());
        _sut.Enqueue(new PollRequested());
        _sut.Enqueue(new MqttConnected());

        // Assert
        Assert.Equal([new PollRequested(), new MqttConnected()], await ReadAsync(2));
    }

    [Fact]
    public async Task Enqueue_PollAfterPreviousPollWasRead_IsQueuedAgain()
    {
        // Arrange
        _sut.Enqueue(new PollRequested());
        await ReadAsync(1);

        // Act
        _sut.Enqueue(new PollRequested());

        // Assert
        Assert.Equal([new PollRequested()], await ReadAsync(1));
    }

    private async Task<List<BridgeCommand>> ReadAsync(int count)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = new List<BridgeCommand>();
        await foreach (var command in _sut.ReadAllAsync(cts.Token))
        {
            result.Add(command);
            if (result.Count == count)
                break;
        }

        return result;
    }
}