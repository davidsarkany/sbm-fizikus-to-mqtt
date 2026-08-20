using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet;
using SbmFizikusToMqtt.MqttConnector.Services;

namespace SbmFizikusToMqtt.MqttConnector.Tests.Services;

public sealed class MqttConnectionServiceTests
{
    private readonly Mock<IHostApplicationLifetime> _hostApplicationLifetimeMock;
    private readonly Mock<ILogger<MqttConnectionService>> _loggerMock;
    private readonly Mock<IMqttClient> _mqttClientMock;
    private readonly MqttClientOptions _mqttClientOptions;

    public MqttConnectionServiceTests()
    {
        _mqttClientMock = new Mock<IMqttClient>();
        _mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .Build();
        _loggerMock = new Mock<ILogger<MqttConnectionService>>();
        _hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
    }

    private MqttConnectionService CreateSut()
    {
        return new MqttConnectionService(
            _mqttClientMock.Object,
            _mqttClientOptions,
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
    public async Task WaitUntilConnectedAsync_AfterStartAsync_CompletesImmediately()
    {
        // Arrange
        var sut = CreateSut();

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        await sut.StartAsync(CancellationToken.None);

        // Act
        var waitTask = sut.WaitUntilConnectedAsync(CancellationToken.None);

        // Assert
        Assert.True(waitTask.IsCompletedSuccessfully);
        await waitTask;
    }

    [Fact]
    public async Task WaitUntilConnectedAsync_BeforeStartAsync_WaitsUntilConnectionIsEstablished()
    {
        // Arrange
        var sut = CreateSut();

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        // Act
        var waitTask = sut.WaitUntilConnectedAsync(CancellationToken.None);

        Assert.False(waitTask.IsCompleted);

        await sut.StartAsync(CancellationToken.None);
        await waitTask;
    }

    [Fact]
    public async Task WaitUntilConnectedAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.WaitUntilConnectedAsync(cts.Token));
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
    public async Task HandleDisconnectedAsync_GracefulDisconnect_DoesNotStopApplication()
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
        _hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Never);
    }

    [Fact]
    public async Task HandleDisconnectedAsync_ClientWasNotConnected_DoesNotStopApplication()
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
        _hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Never);
    }

    [Fact]
    public async Task HandleDisconnectedAsync_UnexpectedDisconnect_StopsApplication()
    {
        // Arrange
        var sut = CreateSut();

        _mqttClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult());

        await sut.StartAsync(CancellationToken.None);

        var disconnectArgs = new MqttClientDisconnectedEventArgs(
            true, null, MqttClientDisconnectReason.UnspecifiedError, null, null, null);

        // Act
        await sut.HandleDisconnectedAsync(disconnectArgs);

        // Assert
        _hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }
}