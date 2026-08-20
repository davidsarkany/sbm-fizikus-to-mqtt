using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SbmFizikusToMqtt.Application.BackgroundJobs;
using SbmFizikusToMqtt.Application.Tests.Fakers;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.Tests.BackgroundJobs;

public sealed class InitialSbmPollingJobTests
{

    private readonly Mock<IApartmentService> _apartmentServiceMock;
    private readonly Mock<ILogger<InitialSbmPollingJob>> _loggerMock;
    private readonly Mock<IMqttConnection> _mqttConnectionMock;
    private readonly Mock<IMqttPublisher> _publisherMock;

    public InitialSbmPollingJobTests()
    {
        _mqttConnectionMock = new Mock<IMqttConnection>();
        _apartmentServiceMock = new Mock<IApartmentService>();
        _publisherMock = new Mock<IMqttPublisher>();
        _loggerMock = new Mock<ILogger<InitialSbmPollingJob>>();

        _mqttConnectionMock
            .Setup(x => x.WaitUntilConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }


    [Fact]
    public async Task ExecuteAsync_MqttConnected_FetchesAndPublishesApartment()
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
        await InvokeExecuteAsync(sut, CancellationToken.None);

        // Assert
        _mqttConnectionMock.Verify(x => x.WaitUntilConnectedAsync(It.IsAny<CancellationToken>()), Times.Once);
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(apartment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MqttNotConnectedInitially_WaitsForConnection()
    {
        // Arrange
        _mqttConnectionMock
            .Setup(x => x.WaitUntilConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken _) => await Task.Delay(50));

        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateJob();

        // Act
        await InvokeExecuteAsync(sut, CancellationToken.None);

        // Assert
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(apartment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledWhileWaitingForConnection_DoesNotFetchOrPublish()
    {
        // Arrange
        _mqttConnectionMock
            .Setup(x => x.WaitUntilConnectedAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => Task.Delay(Timeout.Infinite, ct));

        var sut = CreateJob();
        var cts = new CancellationTokenSource();

        // Cancel almost immediately
        cts.CancelAfter(50);

        // Act
        try
        {
            await InvokeExecuteAsync(sut, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected - PeriodicTimer.WaitForNextTickAsync throws when cancelled
        }

        // Assert
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OperationCancelledException_LogsWarning()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Cancelled"));

        var sut = CreateJob();

        // Act
        await InvokeExecuteAsync(sut, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<OperationCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HttpRequestException_LogsError()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateJob();

        // Act
        await InvokeExecuteAsync(sut, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UnexpectedException_LogsError()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var sut = CreateJob();

        // Act
        await InvokeExecuteAsync(sut, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _publisherMock.Verify(x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PublishThrowsException_LogsError()
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

        // Act
        await InvokeExecuteAsync(sut, CancellationToken.None);

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
    public async Task ExecuteAsync_MqttConnected_LogsInformationMessages()
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
        await InvokeExecuteAsync(sut, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    private InitialSbmPollingJob CreateJob()
    {
        return new InitialSbmPollingJob(
            _mqttConnectionMock.Object,
            _apartmentServiceMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    private static Task InvokeExecuteAsync(InitialSbmPollingJob job, CancellationToken cancellationToken)
    {
        // Use reflection to invoke the protected ExecuteAsync method
        var method = typeof(BackgroundService).GetMethod("ExecuteAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("ExecuteAsync method not found");

        return (Task)method.Invoke(job, [cancellationToken])!;
    }
}