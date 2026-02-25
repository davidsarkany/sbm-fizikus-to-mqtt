using Bogus;
using Microsoft.Extensions.Logging;
using Moq;
using SbmFizikusToMqtt.Application.ScheduledJobs;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.Tests.ScheduledJobs;

public sealed class SbmPollingJobTests
{
    private static readonly Faker<Apartment> ApartmentFaker = new Faker<Apartment>()
        .RuleFor(x => x.SystemMode, f => f.PickRandom("heating", "cooling", "off"))
        .RuleFor(x => x.Thermostats, f => GenerateThermostats(f.Random.Int(1, 3)))
        .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset())
        .RuleFor(x => x.RelayConnectionActive, f => f.Random.Bool())
        .RuleFor(x => x.ThermostatsConnectionActive, f => f.Random.Bool());

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
    public async Task PollSbmData_ValidResponse_FetchesApartmentAndPublishes()
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
        await sut.PollSbmData(default!, CancellationToken.None);

        // Assert
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.Publish(apartment, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PollSbmData_ValidResponse_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var apartment = ApartmentFaker.Generate();

        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(token))
            .ReturnsAsync(apartment);
        _publisherMock
            .Setup(x => x.Publish(apartment, token))
            .Returns(Task.CompletedTask);

        var sut = CreateJob();

        // Act
        await sut.PollSbmData(default!, token);

        // Assert
        _apartmentServiceMock.Verify(x => x.GetApartmentInfo(token), Times.Once);
        _publisherMock.Verify(x => x.Publish(apartment, token), Times.Once);

        cts.Dispose();
    }

    [Fact]
    public async Task PollSbmData_OperationCancelled_LogsWarningAndThrows()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Cancelled"));

        var sut = CreateJob();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.PollSbmData(default!, CancellationToken.None));

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
    public async Task PollSbmData_OperationCancelled_DoesNotPublish()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Cancelled"));

        var sut = CreateJob();

        // Act
        try
        {
            await sut.PollSbmData(default!, CancellationToken.None);
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
    public async Task PollSbmData_HttpRequestException_LogsErrorAndThrows()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateJob();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.PollSbmData(default!, CancellationToken.None));

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
    public async Task PollSbmData_HttpRequestException_DoesNotPublish()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = CreateJob();

        // Act
        try
        {
            await sut.PollSbmData(default!, CancellationToken.None);
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
    public async Task PollSbmData_UnexpectedException_LogsErrorAndThrows()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var sut = CreateJob();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PollSbmData(default!, CancellationToken.None));

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
    public async Task PollSbmData_UnexpectedException_DoesNotPublish()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        var sut = CreateJob();

        // Act
        try
        {
            await sut.PollSbmData(default!, CancellationToken.None);
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
    public async Task PollSbmData_PublishThrowsException_LogsErrorAndThrows()
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

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => sut.PollSbmData(default!, CancellationToken.None));

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
    public async Task PollSbmData_SuccessfulPoll_DoesNotLogWarningOrError()
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
        await sut.PollSbmData(default!, CancellationToken.None);

        // Assert
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
    public async Task PollSbmData_TaskCanceledException_LogsWarningAndThrows()
    {
        // Arrange
        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Task cancelled"));

        var sut = CreateJob();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.PollSbmData(default!, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<TaskCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private SbmPollingJob CreateJob()
    {
        return new SbmPollingJob(
            _apartmentServiceMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }
}