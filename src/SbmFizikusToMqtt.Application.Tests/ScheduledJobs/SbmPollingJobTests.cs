using Microsoft.Extensions.Logging;
using Moq;
using SbmFizikusToMqtt.Application.ScheduledJobs;
using SbmFizikusToMqtt.Application.Tests.Fakers;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;
using TickerQ.Utilities.Base;

namespace SbmFizikusToMqtt.Application.Tests.ScheduledJobs;

public sealed class SbmPollingJobTests
{

    private readonly Mock<IApartmentService> _apartmentServiceMock;
    private readonly Mock<ILogger<SbmPollingJob>> _loggerMock;
    private readonly Mock<IMqttPublisher> _publisherMock;

    public SbmPollingJobTests()
    {
        _apartmentServiceMock = new Mock<IApartmentService>();
        _publisherMock = new Mock<IMqttPublisher>();
        _loggerMock = new Mock<ILogger<SbmPollingJob>>();

        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }


    [Fact]
    public async Task ExecuteAsync_ValidResponse_FetchesApartmentAndPublishes()
    {
        // Arrange
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateJob();

        // Act
        await sut.ExecuteAsync(default!, CancellationToken.None);

        // Assert
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(apartment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ValidResponse_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var apartment = ApartmentFakers.ApartmentFaker.Generate();

        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(token))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, token))
            .Returns(Task.CompletedTask);

        var sut = CreateJob();

        // Act
        await sut.ExecuteAsync(default!, token);

        // Assert
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(token), Times.Once);
        _publisherMock.Verify(x => x.Publish(apartment, token), Times.Once);

        cts.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_OperationCancelled_LogsWarningAndThrows()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Cancelled"));

        var sut = CreateJob();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(default!, CancellationToken.None));

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
    public async Task ExecuteAsync_OperationCancelled_DoesNotPublish()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Cancelled"));

        var sut = CreateJob();

        // Act
        try
        {
            await sut.ExecuteAsync(default!, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HttpRequestException_LogsErrorAndThrows()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateJob();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.ExecuteAsync(default!, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_HttpRequestException_DoesNotPublish()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateJob();

        // Act
        try
        {
            await sut.ExecuteAsync(default!, CancellationToken.None);
        }
        catch (HttpRequestException)
        {
            // Expected
        }

        // Assert
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UnexpectedException_LogsErrorAndThrows()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var sut = CreateJob();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(default!, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UnexpectedException_DoesNotPublish()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var sut = CreateJob();

        // Act
        try
        {
            await sut.ExecuteAsync(default!, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PublishThrowsException_LogsErrorAndThrows()
    {
        // Arrange
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Publish failed"));

        var sut = CreateJob();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => sut.ExecuteAsync(default!, CancellationToken.None));

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
    public async Task ExecuteAsync_SuccessfulPoll_LogsDebugAndDoesNotLogWarningOrError()
    {
        // Arrange
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateJob();

        // Act
        await sut.ExecuteAsync(default!, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
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
    public async Task ExecuteAsync_TaskCanceledException_LogsWarningAndThrows()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Task cancelled"));

        var sut = CreateJob();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.ExecuteAsync(default!, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<TaskCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulPoll_LogsTraceWhenEnabled()
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

        var sut = CreateJob();

        // Act
        await sut.ExecuteAsync(default!, CancellationToken.None);

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
    public async Task ExecuteAsync_SuccessfulPoll_DoesNotLogTraceWhenDisabled()
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

        var sut = CreateJob();

        // Act
        await sut.ExecuteAsync(default!, CancellationToken.None);

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

    private SbmPollingJob CreateJob()
    {
        return new SbmPollingJob(
            _apartmentServiceMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }
}