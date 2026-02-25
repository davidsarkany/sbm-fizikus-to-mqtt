using System.Reflection;
using Bogus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet;
using SbmFizikusToMqtt.Application.BackgroundJobs;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.Tests.BackgroundJobs;

public sealed class InitialSbmPollingJobTests
{
    private static readonly Faker<Apartment> ApartmentFaker = new Faker<Apartment>()
        .RuleFor(x => x.SystemMode, f => f.PickRandom("heating", "cooling", "off"))
        .RuleFor(x => x.Thermostats, f => GenerateThermostats(f.Random.Int(1, 3)))
        .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset())
        .RuleFor(x => x.RelayConnectionActive, f => f.Random.Bool())
        .RuleFor(x => x.ThermostatsConnectionActive, f => f.Random.Bool());

    private readonly Mock<IApartmentService> _apartmentServiceMock;
    private readonly Mock<ILogger<InitialSbmPollingJob>> _loggerMock;
    private readonly Mock<IMqttClient> _mqttClientMock;
    private readonly Mock<IMqttPublisher> _publisherMock;

    public InitialSbmPollingJobTests()
    {
        _mqttClientMock = new Mock<IMqttClient>();
        _apartmentServiceMock = new Mock<IApartmentService>();
        _publisherMock = new Mock<IMqttPublisher>();
        _loggerMock = new Mock<ILogger<InitialSbmPollingJob>>();

        _mqttClientMock.Setup(x => x.IsConnected).Returns(true);
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    private static Thermostat[] GenerateThermostats(int count)
    {
        var thermostatFaker = new Faker<Thermostat>()
            .RuleFor(x => x.Id, f => f.Random.Int(1, 1000))
            .RuleFor(x => x.Name, f => f.Name.FirstName())
            .RuleFor(x => x.Temperature, f => f.Random.Double(15, 30))
            .RuleFor(x => x.Humidity, f => f.Random.Double(30, 70))
            .RuleFor(x => x.TargetTemperature, f => f.Random.Double(18, 25))
            .RuleFor(x => x.DewPoint, f => f.Random.Double(5, 15))
            .RuleFor(x => x.Active, f => f.Random.Bool())
            .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset());

        return thermostatFaker.Generate(count).ToArray();
    }

    [Fact]
    public async Task ExecuteAsync_MqttConnected_FetchesAndPublishesApartment()
    {
        // Arrange
        var apartment = ApartmentFaker.Generate();
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
    public async Task ExecuteAsync_MqttNotConnectedInitially_WaitsForConnection()
    {
        // Arrange
        var callCount = 0;
        _mqttClientMock.Setup(x => x.IsConnected)
            .Returns(() =>
            {
                callCount++;
                return callCount > 3;
            });

        var apartment = ApartmentFaker.Generate();
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
        _mqttClientMock.Setup(x => x.IsConnected).Returns(false);

        var sut = CreateJob();
        var cts = new CancellationTokenSource();

        // Cancel almost immediately
        cts.CancelAfter(50);

        // Act
        try
        {
            await InvokeExecuteAsync(sut, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Expected - Task.Delay throws when cancelled
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
        var apartment = ApartmentFaker.Generate();
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
        var apartment = ApartmentFaker.Generate();
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
            _mqttClientMock.Object,
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