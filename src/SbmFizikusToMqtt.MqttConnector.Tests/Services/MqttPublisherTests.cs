using System.Text;
using Bogus;
using Microsoft.Extensions.Options;
using Moq;
using MQTTnet;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Interfaces;
using SbmFizikusToMqtt.MqttConnector.Configurations;
using SbmFizikusToMqtt.MqttConnector.Domain;
using SbmFizikusToMqtt.MqttConnector.Services;

namespace SbmFizikusToMqtt.MqttConnector.Tests.Services;

public sealed class MqttPublisherTests
{
    private static readonly Faker<Thermostat> ThermostatFaker = new Faker<Thermostat>()
        .RuleFor(x => x.Id, f => f.Random.Int(1, 1000))
        .RuleFor(x => x.Name, f => f.Name.FirstName())
        .RuleFor(x => x.Temperature, f => f.Random.Double(15, 30))
        .RuleFor(x => x.Humidity, f => f.Random.Double(30, 70))
        .RuleFor(x => x.TargetTemperature, f => f.Random.Double(18, 25))
        .RuleFor(x => x.DewPoint, f => f.Random.Double(5, 15))
        .RuleFor(x => x.Active, f => f.Random.Bool())
        .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset());

    private static readonly Faker<Apartment> ApartmentFaker = new Faker<Apartment>()
        .RuleFor(x => x.SystemMode, f => f.PickRandom("heating", "cooling", "off"))
        .RuleFor(x => x.Thermostats, f => ThermostatFaker.Generate(f.Random.Int(1, 3)))
        .RuleFor(x => x.LastUpdate, f => f.Date.RecentOffset())
        .RuleFor(x => x.RelayConnectionActive, f => f.Random.Bool())
        .RuleFor(x => x.ThermostatsConnectionActive, f => f.Random.Bool());

    private readonly Mock<IAutoDiscoveryGenerator> _autoDiscoveryGeneratorMock;
    private readonly MqttConnectorPublisherConfiguration _configuration;
    private readonly Mock<IMqttClient> _mqttClientMock;
    private readonly Mock<IOptionsMonitor<MqttConnectorPublisherConfiguration>> _optionsMonitorMock;

    public MqttPublisherTests()
    {
        _mqttClientMock = new Mock<IMqttClient>();
        _autoDiscoveryGeneratorMock = new Mock<IAutoDiscoveryGenerator>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<MqttConnectorPublisherConfiguration>>();

        _configuration = new MqttConnectorPublisherConfiguration
        {
            SbmTopic = "sbm"
        };

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_configuration);
    }

    [Fact]
    public async Task Publish_ValidApartmentWithActiveConnections_PublishesOnlineState()
    {
        // Arrange
        var apartment = ApartmentFaker
            .RuleFor(x => x.RelayConnectionActive, true)
            .RuleFor(x => x.ThermostatsConnectionActive, true)
            .Generate();

        var sut = CreateMqttPublisher();

        MqttApplicationMessage? capturedStateMessage = null;
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) =>
            {
                if (msg.Topic.EndsWith("/bridge/state")) capturedStateMessage = msg;
            });

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.NotNull(capturedStateMessage);
        Assert.Equal("sbm/bridge/state", capturedStateMessage.Topic);
        var payload = Encoding.UTF8.GetString(capturedStateMessage.Payload);
        Assert.Contains("\"state\": \"online\"", payload);
    }

    [Fact]
    public async Task Publish_ApartmentWithInactiveRelayConnection_PublishesOfflineState()
    {
        // Arrange
        var apartment = ApartmentFaker
            .RuleFor(x => x.RelayConnectionActive, false)
            .RuleFor(x => x.ThermostatsConnectionActive, true)
            .Generate();

        var sut = CreateMqttPublisher();

        MqttApplicationMessage? capturedStateMessage = null;
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) =>
            {
                if (msg.Topic.EndsWith("/bridge/state")) capturedStateMessage = msg;
            });

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.NotNull(capturedStateMessage);
        Assert.Equal("sbm/bridge/state", capturedStateMessage.Topic);
        var payload = Encoding.UTF8.GetString(capturedStateMessage.Payload);
        Assert.Contains("\"state\": \"offline\"", payload);
    }

    [Fact]
    public async Task Publish_ApartmentWithInactiveThermostatsConnection_PublishesOfflineState()
    {
        // Arrange
        var apartment = ApartmentFaker
            .RuleFor(x => x.RelayConnectionActive, true)
            .RuleFor(x => x.ThermostatsConnectionActive, false)
            .Generate();

        var sut = CreateMqttPublisher();

        MqttApplicationMessage? capturedStateMessage = null;
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) =>
            {
                if (msg.Topic.EndsWith("/bridge/state")) capturedStateMessage = msg;
            });

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.NotNull(capturedStateMessage);
        Assert.Equal("sbm/bridge/state", capturedStateMessage.Topic);
        var payload = Encoding.UTF8.GetString(capturedStateMessage.Payload);
        Assert.Contains("\"state\": \"offline\"", payload);
    }

    [Fact]
    public async Task Publish_ValidApartment_PublishesApartmentInfo()
    {
        // Arrange
        var apartment = ApartmentFaker.Generate();
        var sut = CreateMqttPublisher();

        MqttApplicationMessage? capturedApartmentMessage = null;
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) =>
            {
                if (msg.Topic.EndsWith("/apartment_info")) capturedApartmentMessage = msg;
            });

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.NotNull(capturedApartmentMessage);
        Assert.Equal("sbm/apartment_info", capturedApartmentMessage.Topic);
        var payload = Encoding.UTF8.GetString(capturedApartmentMessage.Payload);
        Assert.Contains("\"system_mode\"", payload);
        Assert.Contains("\"last_update\"", payload);
        Assert.Contains(apartment.SystemMode, payload);
    }

    [Fact]
    public async Task Publish_ApartmentWithThreeThermostats_PublishesThreeThermostatMessages()
    {
        // Arrange
        var apartment = ApartmentFaker
            .RuleFor(x => x.Thermostats, ThermostatFaker.Generate(3))
            .Generate();

        var sut = CreateMqttPublisher();

        var thermostatMessages = new List<MqttApplicationMessage>();
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) =>
            {
                if (msg.Topic.StartsWith("sbm/devices/")) thermostatMessages.Add(msg);
            });

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.Equal(3, thermostatMessages.Count);
        foreach (var thermostat in apartment.Thermostats)
        {
            var message = thermostatMessages.FirstOrDefault(m => m.Topic == $"sbm/devices/{thermostat.Id}");
            Assert.NotNull(message);

            var payload = Encoding.UTF8.GetString(message.Payload);
            Assert.Contains("\"id\"", payload);
            Assert.Contains("\"temperature\"", payload);
            Assert.Contains("\"humidity\"", payload);
            Assert.Contains("\"target_temperature\"", payload);
            Assert.Contains("\"system_mode\"", payload);
        }
    }

    [Fact]
    public async Task Publish_FirstCall_PublishesAutoDiscovery()
    {
        // Arrange
        var apartment = ApartmentFaker.Generate();
        var discoveryMessages = new List<MqttMessage>
        {
            new() { Topic = "homeassistant/sensor/test/config", Payload = "{}" },
            new() { Topic = "homeassistant/climate/test/config", Payload = "{}" }
        };

        _autoDiscoveryGeneratorMock
            .Setup(x => x.Generate(It.IsAny<Apartment>()))
            .Returns(discoveryMessages);

        var sut = CreateMqttPublisher();

        // Act
        await sut.PublishHomeAssistantAutoDiscovery(apartment);

        // Assert
        _autoDiscoveryGeneratorMock.Verify(x => x.Generate(apartment), Times.Once);
        _mqttClientMock.Verify(
            x => x.PublishAsync(
                It.Is<MqttApplicationMessage>(m => m.Topic == "homeassistant/sensor/test/config"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mqttClientMock.Verify(
            x => x.PublishAsync(
                It.Is<MqttApplicationMessage>(m => m.Topic == "homeassistant/climate/test/config"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Publish_SecondCall_PublishesAutoDiscoveryAgain()
    {
        // Arrange
        var apartment = ApartmentFaker.Generate();
        var discoveryMessages = new List<MqttMessage>
        {
            new() { Topic = "homeassistant/sensor/test/config", Payload = "{}" }
        };

        _autoDiscoveryGeneratorMock
            .Setup(x => x.Generate(It.IsAny<Apartment>()))
            .Returns(discoveryMessages);

        var sut = CreateMqttPublisher();

        // Act
        await sut.PublishHomeAssistantAutoDiscovery(apartment);
        await sut.PublishHomeAssistantAutoDiscovery(apartment);

        // Assert
        _autoDiscoveryGeneratorMock.Verify(x => x.Generate(It.IsAny<Apartment>()), Times.Exactly(2));
        _mqttClientMock.Verify(
            x => x.PublishAsync(
                It.Is<MqttApplicationMessage>(m => m.Topic == "homeassistant/sensor/test/config"),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Publish_AutoDiscoveryMessages_SetsRetainFlag()
    {
        // Arrange
        var apartment = ApartmentFaker.Generate();
        var discoveryMessages = new List<MqttMessage>
        {
            new() { Topic = "homeassistant/sensor/test/config", Payload = "{}" }
        };

        _autoDiscoveryGeneratorMock
            .Setup(x => x.Generate(It.IsAny<Apartment>()))
            .Returns(discoveryMessages);

        var sut = CreateMqttPublisher();

        MqttApplicationMessage? capturedDiscoveryMessage = null;
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) =>
            {
                if (msg.Topic.StartsWith("homeassistant/")) capturedDiscoveryMessage = msg;
            });

        // Act
        await sut.PublishHomeAssistantAutoDiscovery(apartment);

        // Assert
        Assert.NotNull(capturedDiscoveryMessage);
        Assert.True(capturedDiscoveryMessage.Retain);
    }

    [Fact]
    public async Task Publish_WithCancellationToken_PassesTokenToMqttClient()
    {
        // Arrange
        var apartment = ApartmentFaker.Generate();
        var sut = CreateMqttPublisher();
        var cancellationToken = new CancellationToken();

        // Act
        await sut.Publish(apartment, cancellationToken);

        // Assert
        _mqttClientMock.Verify(
            x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), cancellationToken),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Publish_ActiveThermostat_PublishesCorrectSystemMode()
    {
        // Arrange
        var thermostat = ThermostatFaker
            .RuleFor(x => x.Active, true)
            .RuleFor(x => x.Id, 123)
            .Generate();

        var apartment = ApartmentFaker
            .RuleFor(x => x.SystemMode, "heating")
            .RuleFor(x => x.Thermostats, new[] { thermostat })
            .Generate();

        var sut = CreateMqttPublisher();

        MqttApplicationMessage? capturedThermostatMessage = null;
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) =>
            {
                if (msg.Topic == "sbm/devices/123") capturedThermostatMessage = msg;
            });

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.NotNull(capturedThermostatMessage);
        var payload = Encoding.UTF8.GetString(capturedThermostatMessage.Payload);
        Assert.Contains("\"system_mode\":\"heating\"", payload);
    }

    [Fact]
    public async Task Publish_InactiveThermostat_PublishesIdleMode()
    {
        // Arrange
        var thermostat = ThermostatFaker
            .RuleFor(x => x.Active, false)
            .RuleFor(x => x.Id, 456)
            .Generate();

        var apartment = ApartmentFaker
            .RuleFor(x => x.SystemMode, "heating")
            .RuleFor(x => x.Thermostats, new[] { thermostat })
            .Generate();

        var sut = CreateMqttPublisher();

        MqttApplicationMessage? capturedThermostatMessage = null;
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) =>
            {
                if (msg.Topic == "sbm/devices/456") capturedThermostatMessage = msg;
            });

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.NotNull(capturedThermostatMessage);
        var payload = Encoding.UTF8.GetString(capturedThermostatMessage.Payload);
        Assert.Contains("\"system_mode\":\"idle\"", payload);
    }

    [Fact]
    public async Task Publish_ValidApartment_PublishesAllMessageTypes()
    {
        // Arrange
        var apartment = ApartmentFaker
            .RuleFor(x => x.Thermostats, ThermostatFaker.Generate(2))
            .Generate();

        var sut = CreateMqttPublisher();

        var publishedTopics = new List<string>();
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) => publishedTopics.Add(msg.Topic));

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.Contains(publishedTopics, t => t.EndsWith("/bridge/state"));
        Assert.Contains(publishedTopics, t => t.EndsWith("/apartment_info"));
        Assert.Equal(2, publishedTopics.Count(t => t.StartsWith("sbm/devices/")));
    }

    [Fact]
    public async Task Publish_EmptyThermostats_PublishesStateAndApartmentInfoOnly()
    {
        // Arrange
        var apartment = ApartmentFaker
            .RuleFor(x => x.Thermostats, Array.Empty<Thermostat>())
            .Generate();

        var sut = CreateMqttPublisher();

        var publishedTopics = new List<string>();
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) => publishedTopics.Add(msg.Topic));

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.Contains(publishedTopics, t => t.EndsWith("/bridge/state"));
        Assert.Contains(publishedTopics, t => t.EndsWith("/apartment_info"));
        Assert.DoesNotContain(publishedTopics, t => t.StartsWith("sbm/devices/"));
    }

    [Fact]
    public async Task Publish_CustomSbmTopic_UsesCorrectTopicPrefix()
    {
        // Arrange
        var customConfiguration = new MqttConnectorPublisherConfiguration
        {
            SbmTopic = "custom/topic"
        };
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(customConfiguration);

        var apartment = ApartmentFaker.Generate();
        var sut = CreateMqttPublisher();

        var publishedTopics = new List<string>();
        _mqttClientMock
            .Setup(x => x.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MqttApplicationMessage, CancellationToken>((msg, _) => publishedTopics.Add(msg.Topic));

        // Act
        await sut.Publish(apartment);

        // Assert
        Assert.Contains(publishedTopics, t => t == "custom/topic/bridge/state");
        Assert.Contains(publishedTopics, t => t == "custom/topic/apartment_info");
        Assert.All(publishedTopics.Where(t => t.Contains("/devices/")),
            topic => Assert.StartsWith("custom/topic/devices/", topic));
    }

    private MqttPublisher CreateMqttPublisher()
    {
        return new MqttPublisher(
            _mqttClientMock.Object,
            _autoDiscoveryGeneratorMock.Object,
            _optionsMonitorMock.Object);
    }
}