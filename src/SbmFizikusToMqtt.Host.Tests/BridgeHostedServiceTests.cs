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