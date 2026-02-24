using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet;
using SbmFizikusToMqtt.MqttConnector.Services;

namespace SbmFizikusToMqtt.MqttConnector.Tests.Services;

public sealed class MqttConnectionServiceTests
{
    private readonly Mock<IMqttClient> _mqttClientMock;
    private readonly MqttClientOptions _mqttClientOptions;
    private readonly Mock<ILogger<MqttConnectionService>> _loggerMock;

    public MqttConnectionServiceTests()
    {
        _mqttClientMock = new Mock<IMqttClient>();
        _mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .Build();
        _loggerMock = new Mock<ILogger<MqttConnectionService>>();
    }

    private MqttConnectionService CreateSut() =>
        new(_mqttClientMock.Object, _mqttClientOptions, _loggerMock.Object);

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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.StartAsync(CancellationToken.None));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task StopAsync_WhenConnected_DisconnectsFromBroker()
    {
        // Arrange
        var sut = CreateSut();
        var cancellationToken = CancellationToken.None;

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
            .Setup(x => x.IsConnected)
            .Returns(true);

        _mqttClientMock
            .Setup(x => x.DisconnectAsync(It.IsAny<MqttClientDisconnectOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.StopAsync(CancellationToken.None));

        Assert.Same(expectedException, exception);
    }
}





