using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Core.Publishing;
using SbmFizikusToMqtt.Host.Configuration;

namespace SbmFizikusToMqtt.Host.Tests;

public sealed class BridgeHostedServiceTests
{
    [Fact]
    public async Task StopAsync_CommandStillCancelling_WaitsForCommandCompletion()
    {
        // Arrange
        var sbm = new SlowCancellationSbmGateway();
        var queue = new BridgeCommandQueue();
        var status = new BridgeStatus(TimeProvider.System);
        var topics = new MqttTopics(new TopicOptions { SbmTopic = "sbm_fizikus", HomeAssistantTopic = "homeassistant" });
        var processor = new BridgeProcessor(sbm, new NoOpMqttGateway(), status, topics,
            new DiscoveryOptions { SoftwareVersion = "test" }, TimeProvider.System,
            NullLogger<BridgeProcessor>.Instance);
        using var sut = new BridgeHostedService(processor, queue, status,
            Options.Create(new SbmConnectorSettings()), TimeProvider.System,
            NullLogger<BridgeHostedService>.Instance);
        queue.Enqueue(new PollRequested());
        await sut.StartAsync(TestContext.Current.CancellationToken);
        await sbm.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        try
        {
            // Act
            var stopped = sut.StopAsync(TestContext.Current.CancellationToken);
            await sbm.Cancelling.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            // Assert
            await Assert.ThrowsAsync<TimeoutException>(() => stopped.WaitAsync(TimeSpan.FromMilliseconds(250),
                TestContext.Current.CancellationToken));
            sbm.AllowCompletion.TrySetResult();
            await stopped.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(sbm.Completed);
        }
        finally
        {
            sbm.AllowCompletion.TrySetResult();
            await sut.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CommandLoopFails_FaultsWithoutWaitingForShutdown()
    {
        // Arrange
        var queue = new BridgeCommandQueue();
        var status = new BridgeStatus(TimeProvider.System);
        var topics = new MqttTopics(new TopicOptions { SbmTopic = "sbm_fizikus", HomeAssistantTopic = "homeassistant" });
        // BridgeProcessor catches every failure, so a throwing logger is the way to make it break.
        var processor = new BridgeProcessor(new FailingSbmGateway(), new NoOpMqttGateway(), status, topics,
            new DiscoveryOptions { SoftwareVersion = "test" }, TimeProvider.System,
            new ThrowingLogger<BridgeProcessor>());
        using var sut = new BridgeHostedService(processor, queue, status,
            Options.Create(new SbmConnectorSettings()), TimeProvider.System,
            NullLogger<BridgeHostedService>.Instance);
        var originalExitCode = Environment.ExitCode;

        try
        {
            // Act
            queue.Enqueue(new PollRequested());
            await sut.StartAsync(TestContext.Current.CancellationToken);
            var exception = await Record.ExceptionAsync(() =>
                sut.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

            // Assert
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Equal("logger failed", exception.Message);
            Assert.Equal(1, Environment.ExitCode);
        }
        finally
        {
            Environment.ExitCode = originalExitCode;
            await sut.StopAsync(CancellationToken.None);
        }
    }

    private sealed class FailingSbmGateway : ISbmGateway
    {
        public Task<Apartment> GetApartmentAsync(CancellationToken cancellationToken)
        {
            return Task.FromException<Apartment>(new HttpRequestException("SBM unavailable"));
        }

        public Task SetTargetTemperatureAsync(int thermostatId, double temperature,
            CancellationToken cancellationToken)
        {
            return Task.FromException(new HttpRequestException("SBM unavailable"));
        }
    }

    private sealed class SlowCancellationSbmGateway : ISbmGateway
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelling { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Completed { get; private set; }

        public async Task<Apartment> GetApartmentAsync(CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            finally
            {
                Cancelling.TrySetResult();
                // Model asynchronous cleanup that must finish even after cancellation.
                await AllowCompletion.Task;
                Completed = true;
            }

            throw new InvalidOperationException("Expected cancellation");
        }

        public Task SetTargetTemperatureAsync(int thermostatId, double temperature, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class NoOpMqttGateway : IMqttGateway
    {
        public Task PublishAsync(MqttMessage message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
                throw new InvalidOperationException("logger failed");
        }
    }
}