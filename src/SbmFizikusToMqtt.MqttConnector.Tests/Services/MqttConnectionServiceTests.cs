using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet;
using SbmFizikusToMqtt.MqttConnector.Configurations;
using SbmFizikusToMqtt.MqttConnector.Services;

namespace SbmFizikusToMqtt.MqttConnector.Tests.Services;

public sealed class MqttConnectionServiceTests
{
    private readonly Mock<IHostApplicationLifetime> _hostApplicationLifetimeMock;
    private readonly Mock<ILogger<MqttConnectionService>> _loggerMock;
    private readonly Mock<IMqttClient> _mqttClientMock;
    private readonly MqttClientOptions _mqttClientOptions;
    private readonly MqttReconnectConfiguration _reconnectConfiguration;

    public MqttConnectionServiceTests()
    {
        _mqttClientMock = new Mock<IMqttClient>();
        _mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .Build();
        _loggerMock = new Mock<ILogger<MqttConnectionService>>();
        _hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        _reconnectConfiguration = new MqttReconnectConfiguration
        {
            MaxReconnectAttempts = 3,
            InitialDelaySeconds = 1,
            MaxDelaySeconds = 10
        };
    }

    private MqttConnectionService CreateSut()
    {
        return new MqttConnectionService(
            _mqttClientMock.Object,
            _mqttClientOptions,
            _reconnectConfiguration,
            _hostApplicationLifetimeMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task StartAsync_ValidOptions_ConnectsToBroker()
    {
        // Arrange
        var sut = CreateSut();
        var cancellationToken = CancellationToken.None;

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        // Act
        await sut.StartAsync(cancellationToken);

        // Assert
        _mqttClientMock.Verify(
            x => x.ConnectAsync(_mqttClientOptions, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithCancellationToken_PassesTokenToConnect()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var sut = CreateSut();

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        // Act
        await sut.StartAsync(cts.Token);

        // Assert
        _mqttClientMock.Verify(
            x => x.ConnectAsync(_mqttClientOptions, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_BrokerThrowsException_PropagatesException()
    {
        // Arrange
        var sut = CreateSut();
        var expectedException = new InvalidOperationException("Connection refused");

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.StartAsync(CancellationToken.None));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task StopAsync_WhenConnected_DisconnectsFromBroker()
    {
        // Arrange
        var sut = CreateSut();
        var cancellationToken = CancellationToken.None;

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        await sut.StartAsync(cancellationToken);

        _mqttClientMock
            .Setup(x => x.IsConnected)
            .Returns(true);

        // Act
        await sut.StopAsync(cancellationToken);

        // Assert
        _mqttClientMock.Verify(
            x => x.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenNotConnected_DoesNotDisconnect()
    {
        // Arrange
        var sut = CreateSut();

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        await sut.StartAsync(CancellationToken.None);

        _mqttClientMock
            .Setup(x => x.IsConnected)
            .Returns(false);

        // Act
        await sut.StopAsync(CancellationToken.None);

        // Assert
        _mqttClientMock.Verify(
            x => x.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StopAsync_WithCancellationToken_PassesTokenToDisconnect()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var sut = CreateSut();

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        await sut.StartAsync(cts.Token);

        _mqttClientMock
            .Setup(x => x.IsConnected)
            .Returns(true);

        // Act
        await sut.StopAsync(cts.Token);

        // Assert
        _mqttClientMock.Verify(
            x => x.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_DisconnectThrowsException_PropagatesException()
    {
        // Arrange
        var sut = CreateSut();
        var expectedException = new InvalidOperationException("Disconnect failed");

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        await sut.StartAsync(CancellationToken.None);

        _mqttClientMock
            .Setup(x => x.IsConnected)
            .Returns(true);

        _mqttClientMock
            .Setup(x => x.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.StopAsync(CancellationToken.None));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task HandleDisconnectedAsync_GracefulDisconnect_DoesNotReconnect()
    {
        // Arrange
        var sut = CreateSut();

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        await sut.StartAsync(CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        var disconnectArgs = new MqttClientDisconnectedEventArgs(
            true, null, MqttClientDisconnectReason.NormalDisconnection, null, null, null);

        // Act
        await sut.HandleDisconnectedAsync(disconnectArgs);

        // Assert
        // ConnectAsync was called once during StartAsync, not again after disconnect
        _mqttClientMock.Verify(
            x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleDisconnectedAsync_ClientWasNotConnected_DoesNotReconnect()
    {
        // Arrange
        var sut = CreateSut();

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        await sut.StartAsync(CancellationToken.None);

        var disconnectArgs = new MqttClientDisconnectedEventArgs(
            false, null, MqttClientDisconnectReason.NormalDisconnection, null, null, null);

        // Act
        await sut.HandleDisconnectedAsync(disconnectArgs);

        // Assert
        _mqttClientMock.Verify(
            x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleDisconnectedAsync_ReconnectSucceedsOnFirstAttempt_DoesNotStopApplication()
    {
        // Arrange
        var sut = CreateSut();
        var callCount = 0;

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new MqttClientConnectResult();
            });

        await sut.StartAsync(CancellationToken.None);

        var disconnectArgs = new MqttClientDisconnectedEventArgs(
            true, null, MqttClientDisconnectReason.UnspecifiedError, null, null, null);

        // Act
        await sut.HandleDisconnectedAsync(disconnectArgs);

        // Assert
        Assert.Equal(2, callCount); // Once for StartAsync, once for reconnect
        _hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Never);
    }

    [Fact]
    public async Task HandleDisconnectedAsync_AllReconnectAttemptsFail_StopsApplication()
    {
        // Arrange
        var sut = CreateSut();
        var connectCallCount = 0;

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectCallCount++;
                if (connectCallCount == 1)
                    return new MqttClientConnectResult(); // Initial connect succeeds
                throw new InvalidOperationException("Connection refused");
            });

        await sut.StartAsync(CancellationToken.None);

        var disconnectArgs = new MqttClientDisconnectedEventArgs(
            true, null, MqttClientDisconnectReason.UnspecifiedError, null, null, null);

        // Act
        await sut.HandleDisconnectedAsync(disconnectArgs);

        // Assert
        // 1 initial + 3 reconnect attempts
        Assert.Equal(4, connectCallCount);
        _hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [Fact]
    public async Task HandleDisconnectedAsync_ReconnectSucceedsOnSecondAttempt_DoesNotStopApplication()
    {
        // Arrange
        var sut = CreateSut();
        var connectCallCount = 0;

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                connectCallCount++;
                if (connectCallCount <= 2)
                {
                    if (connectCallCount == 2)
                        throw new InvalidOperationException("Connection refused");
                }

                return new MqttClientConnectResult();
            });

        await sut.StartAsync(CancellationToken.None);

        var disconnectArgs = new MqttClientDisconnectedEventArgs(
            true, null, MqttClientDisconnectReason.UnspecifiedError, null, null, null);

        // Act
        await sut.HandleDisconnectedAsync(disconnectArgs);

        // Assert
        // 1 initial + 1 failed reconnect + 1 successful reconnect
        Assert.Equal(3, connectCallCount);
        _hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Never);
    }

    [Theory]
    [InlineData(1, 1)] // 1 * 2^0 = 1
    [InlineData(2, 2)] // 1 * 2^1 = 2
    [InlineData(3, 4)] // 1 * 2^2 = 4
    [InlineData(4, 8)] // 1 * 2^3 = 8
    [InlineData(5, 10)] // 1 * 2^4 = 16, capped at MaxDelaySeconds=10
    public void CalculateDelay_VariousAttempts_ReturnsExponentialBackoffWithCap(int attempt, int expectedDelay)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var delay = sut.CalculateDelay(attempt);

        // Assert
        Assert.Equal(expectedDelay, delay);
    }
}