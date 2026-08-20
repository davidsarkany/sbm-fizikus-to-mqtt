using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using SbmFizikusToMqtt.Application.Configurations;
using SbmFizikusToMqtt.Application.ScheduledJobs;
using SbmFizikusToMqtt.Application.Tests.Fakers;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.Tests.ScheduledJobs;

public sealed class SbmPollingBackgroundServiceTests
{
    private readonly Mock<IApartmentService> _apartmentServiceMock;
    private readonly Mock<ILogger<SbmPollingBackgroundService>> _loggerMock;
    private readonly Mock<IMqttPublisher> _publisherMock;
    private readonly FakeTimeProvider _timeProvider = new();

    public SbmPollingBackgroundServiceTests()
    {
        _apartmentServiceMock = new Mock<IApartmentService>();
        _publisherMock = new Mock<IMqttPublisher>();
        _loggerMock = new Mock<ILogger<SbmPollingBackgroundService>>();

        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    public async Task PollOnceAsync_ValidResponse_FetchesApartmentAndPublishes()
    {
        // Arrange
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(CancellationToken.None);

        // Assert
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(apartment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PollOnceAsync_ValidResponse_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var apartment = ApartmentFakers.ApartmentFaker.Generate();

        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(token))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, token))
            .Returns(Task.CompletedTask);

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(token);

        // Assert
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(token), Times.Once);
        _publisherMock.Verify(x => x.Publish(apartment, token), Times.Once);
    }

    [Fact]
    public async Task PollOnceAsync_OperationCancelled_LogsWarningAndRethrows()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Cancelled"));

        var sut = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.PollOnceAsync(cts.Token));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<OperationCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PollOnceAsync_OperationCancelledWithoutCancelledToken_LogsWarningAndDoesNotThrow()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Cancelled"));

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(CancellationToken.None);

        // Assert
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PollOnceAsync_TaskCanceledException_LogsWarningAndDoesNotThrow()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Task cancelled"));

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<TaskCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PollOnceAsync_HttpRequestException_LogsErrorAndDoesNotThrow()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PollOnceAsync_UnexpectedException_LogsErrorAndDoesNotThrow()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PollOnceAsync_PublishThrowsException_LogsErrorAndDoesNotThrow()
    {
        // Arrange
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Publish failed"));

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PollOnceAsync_SuccessfulPoll_LogsInformationAndDoesNotLogWarningOrError()
    {
        // Arrange
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task PollOnceAsync_SuccessfulPoll_LogsTraceWhenEnabled()
    {
        // Arrange
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Trace)).Returns(true);

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PollOnceAsync_SuccessfulPoll_DoesNotLogTraceWhenDisabled()
    {
        // Arrange
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Trace)).Returns(false);

        var sut = CreateService();

        // Act
        await sut.PollOnceAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PollsRepeatedlyAtConfiguredInterval()
    {
        SynchronizationContext.SetSynchronizationContext(null);

        // Arrange
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        var pollCount = 0;
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment)
            .Callback(() => Interlocked.Increment(ref pollCount));
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateService(intervalSeconds: 120);
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = InvokeExecuteAsync(sut, cts.Token);

        // No poll happens before the interval elapses
        await Task.Delay(50);
        Assert.Equal(0, pollCount);

        // First poll happens after one interval
        await AdvanceUntilAsync(() => pollCount == 1, TimeSpan.FromSeconds(120));

        // Second poll happens after the next interval
        await AdvanceUntilAsync(() => pollCount == 2, TimeSpan.FromSeconds(120));

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executeTask);

        // Assert
        Assert.Equal(2, pollCount);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledWhileWaitingForNextTick_DoesNotPoll()
    {
        SynchronizationContext.SetSynchronizationContext(null);

        // Arrange
        var sut = CreateService(intervalSeconds: 120);
        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = InvokeExecuteAsync(sut, cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executeTask);

        // Assert
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()), Times.Never);
    }

    private SbmPollingBackgroundService CreateService(int intervalSeconds = 120)
    {
        return new SbmPollingBackgroundService(
            _apartmentServiceMock.Object,
            _publisherMock.Object,
            Options.Create(new PollingConfiguration { PollingIntervalSeconds = intervalSeconds }),
            _timeProvider,
            _loggerMock.Object);
    }

    private async Task AdvanceUntilAsync(Func<bool> condition, TimeSpan advanceStep, int maxSteps = 10)
    {
        for (var i = 0; i < maxSteps && !condition(); i++)
        {
            _timeProvider.Advance(advanceStep);
            await Task.Delay(20);
        }

        Assert.True(condition(), "Expected condition to be reached within the advance window");
    }

    private static Task InvokeExecuteAsync(SbmPollingBackgroundService service, CancellationToken cancellationToken)
    {
        var method = typeof(BackgroundService).GetMethod("ExecuteAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? throw new InvalidOperationException("ExecuteAsync method not found");

        return (Task)method.Invoke(service, new object[] { cancellationToken })!;
    }
}