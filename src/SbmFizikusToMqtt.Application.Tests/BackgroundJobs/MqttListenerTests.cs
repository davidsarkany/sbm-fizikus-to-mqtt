using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Bogus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MQTTnet;
using MQTTnet.Protocol;
using SbmFizikusToMqtt.Application.BackgroundJobs;
using SbmFizikusToMqtt.Application.Tests.Fakers;
using SbmFizikusToMqtt.Domain;
using SbmFizikusToMqtt.HomeAssistantAutoDiscovery.Configurations;
using SbmFizikusToMqtt.MqttConnector.Configurations;
using SbmFizikusToMqtt.MqttConnector.Interfaces;
using SbmFizikusToMqtt.SbmConnector.Interfaces;

namespace SbmFizikusToMqtt.Application.Tests.BackgroundJobs;

public sealed class MqttListenerTests
{

    private readonly Mock<IApartmentService> _apartmentServiceMock;
    private readonly MqttConnectorPublisherConfiguration _configuration;
    private readonly HomeAssistantAutoDiscoveryConfiguration _homeAssistantConfiguration;
    private readonly Mock<ILogger<MqttListener>> _loggerMock;
    private readonly Mock<IMqttClient> _mqttClientMock;
    private readonly Mock<IOptionsMonitor<MqttConnectorPublisherConfiguration>> _optionsMonitorMock;
    private readonly Mock<IMqttPublisher> _publisherMock;
    private readonly Mock<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>> _homeAssistantOptionsMonitorMock;

    public MqttListenerTests()
    {
        _mqttClientMock = new Mock<IMqttClient>();
        _mqttClientMock.SetupAdd(x =>
            x.ApplicationMessageReceivedAsync += It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>());
        _mqttClientMock.SetupRemove(x =>
            x.ApplicationMessageReceivedAsync -= It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>());

        _apartmentServiceMock = new Mock<IApartmentService>();
        _publisherMock = new Mock<IMqttPublisher>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<MqttConnectorPublisherConfiguration>>();
        _homeAssistantOptionsMonitorMock = new Mock<IOptionsMonitor<HomeAssistantAutoDiscoveryConfiguration>>();
        _loggerMock = new Mock<ILogger<MqttListener>>();

        _configuration = new MqttConnectorPublisherConfiguration
        {
            SbmTopic = "sbm"
        };

        _homeAssistantConfiguration = new HomeAssistantAutoDiscoveryConfiguration
        {
            SbmTopic = "homeassistant",
            HomeAssistantTopic = "homeassistant",
            ThermostatTemperatureDiscoveryEnabled = true,
            ThermostatTargetTemperatureDiscoveryEnabled = true,
            ThermostatHumidityDiscoveryEnabled = true,
            ThermostatSystemModeDiscoveryEnabled = true,
            ClimateDiscoveryEnabled = true,
            ApartmentSystemModeDiscoveryEnabled = true,
            ApartmentOutdoorTemperatureDiscoveryEnabled = false,
            ApartmentOutdoorHumidityDiscoveryEnabled = false
        };

        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(_configuration);
        _homeAssistantOptionsMonitorMock.Setup(x => x.CurrentValue).Returns(_homeAssistantConfiguration);
        _mqttClientMock.Setup(x => x.IsConnected).Returns(true);

        // Enable all log levels so that logger.IsEnabled() returns true and logs are actually made
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }


    [Fact]
    public async Task ExecuteAsync_SubscribesToCorrectTopicPattern()
    {
        // Arrange
        var sut = CreateMqttListener();
        var cts = new CancellationTokenSource();
        var capturedOptionsList = new List<MqttClientSubscribeOptions>();

        _mqttClientMock
            .Setup(x => x.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .Callback<MqttClientSubscribeOptions, CancellationToken>((options, _) => capturedOptionsList.Add(options))
            .ReturnsAsync(CreateSubscribeResult());

        // Act
        var executeTask = InvokeExecuteAsync(sut, cts.Token);

        // Wait a bit for the subscription to happen
        await Task.Delay(100);

        // Cancel to stop the service
        cts.Cancel();

        // Wait for the task to complete - it will throw TaskCanceledException from Task.Delay
        try
        {
            await executeTask;
        }
        catch (TaskCanceledException)
        {
            // Expected - the Task.Delay(Timeout.Infinite) throws when cancelled
        }

        // Assert
        Assert.NotEmpty(capturedOptionsList);
        var allTopicFilters = capturedOptionsList.SelectMany(o => o.TopicFilters).ToList();
        var sbmTopicFilter = allTopicFilters.FirstOrDefault(tf => tf.Topic == "sbm/devices/+/set");
        Assert.NotNull(sbmTopicFilter);
        Assert.Equal(MqttQualityOfServiceLevel.AtMostOnce, sbmTopicFilter.QualityOfServiceLevel);
    }

    [Fact]
    public async Task ExecuteAsync_RegistersAndUnregistersMessageHandler()
    {
        // Arrange
        var sut = CreateMqttListener();
        var cts = new CancellationTokenSource();

        _mqttClientMock
            .Setup(x => x.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscribeResult());

        // Act
        var executeTask = InvokeExecuteAsync(sut, cts.Token);
        await Task.Delay(100);

        // Check that handler was registered
        _mqttClientMock.VerifyAdd(
            x => x.ApplicationMessageReceivedAsync += It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>(),
            Times.Once);

        // Cancel to stop the service
        cts.Cancel();

        // Wait for the task to complete - it will throw TaskCanceledException from Task.Delay
        try
        {
            await executeTask;
        }
        catch (TaskCanceledException)
        {
            // Expected - the Task.Delay(Timeout.Infinite) throws when cancelled
        }

        // Assert
        _mqttClientMock.VerifyRemove(
            x => x.ApplicationMessageReceivedAsync -= It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>(),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCanceled_UnsubscribesHandlerAndCompletes()
    {
        // Arrange
        var sut = CreateMqttListener();
        var cts = new CancellationTokenSource();

        _mqttClientMock
            .Setup(x => x.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSubscribeResult());

        // Act
        var executeTask = InvokeExecuteAsync(sut, cts.Token);
        await Task.Delay(100);

        // Cancel the token
        cts.Cancel();

        // Wait for the task to complete - it will throw TaskCanceledException from Task.Delay
        try
        {
            await executeTask;
        }
        catch (TaskCanceledException)
        {
            // Expected - the Task.Delay(Timeout.Infinite) throws when cancelled
        }

        // Assert
        _mqttClientMock.VerifyRemove(
            x => x.ApplicationMessageReceivedAsync -= It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>(),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessageReceived_ValidJsonPayload_ChangesTemperatureAndPublishes()
    {
        // Arrange
        var sut = CreateMqttListener();
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        const int thermostatId = 123;
        const double newTemperature = 22.5;
        // Use invariant culture to ensure dot decimal separator in JSON
        var payload = $"{{\"id\":{thermostatId},\"value\":{newTemperature.ToString(CultureInfo.InvariantCulture)}}}";

        _apartmentServiceMock
            .Setup(x => x.ChangeTemperature(thermostatId, newTemperature, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("sbm/devices/123/set")
            .WithPayload(payload)
            .Build();

        var args = CreateMessageEventArgs("client1", message);

        // Act
        await InvokeHandleMessageReceived(sut, args);

        // Assert
        _apartmentServiceMock.Verify(
            x => x.ChangeTemperature(thermostatId, newTemperature, It.IsAny<CancellationToken>()),
            Times.Once);
        _apartmentServiceMock.Verify(
            x => x.GetApartmentInfo(It.IsAny<CancellationToken>()),
            Times.Once);
        _publisherMock.Verify(
            x => x.Publish(apartment, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessageReceived_NullDeserializedPayload_LogsWarningAndReturns()
    {
        // Arrange
        var sut = CreateMqttListener();
        var payload = "invalid json";

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("sbm/devices/123/set")
            .WithPayload(payload)
            .Build();

        var args = CreateMessageEventArgs("client1", message);

        // Act
        await InvokeHandleMessageReceived(sut, args);

        // Assert
        _apartmentServiceMock.Verify(
            x => x.ChangeTemperature(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _apartmentServiceMock.Verify(
            x => x.GetApartmentInfo(It.IsAny<CancellationToken>()),
            Times.Never);
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Note: The actual implementation logs at Error level when JSON parsing fails (JsonException)
        // inside DeserializeRequest, which catches the exception and logs "Invalid JSON format in payload"
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid JSON format in payload")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessageReceived_InvalidJson_LogsErrorAndReturns()
    {
        // Arrange
        var sut = CreateMqttListener();
        var payload = "{invalid json}";

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("sbm/devices/123/set")
            .WithPayload(payload)
            .Build();

        var args = CreateMessageEventArgs("client1", message);

        // Act
        await InvokeHandleMessageReceived(sut, args);

        // Assert
        _apartmentServiceMock.Verify(
            x => x.ChangeTemperature(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _apartmentServiceMock.Verify(
            x => x.GetApartmentInfo(It.IsAny<CancellationToken>()),
            Times.Never);
        _publisherMock.Verify(
            x => x.Publish(It.IsAny<Apartment>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<JsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessageReceived_ApartmentServiceThrowsException_LogsErrorAndContinues()
    {
        // Arrange
        var sut = CreateMqttListener();
        const int thermostatId = 123;
        const double newTemperature = 22.5;
        var payload = $"{{\"id\":{thermostatId},\"value\":{newTemperature.ToString(CultureInfo.InvariantCulture)}}}";

        _apartmentServiceMock
            .Setup(x => x.ChangeTemperature(thermostatId, newTemperature, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("sbm/devices/123/set")
            .WithPayload(payload)
            .Build();

        var args = CreateMessageEventArgs("client1", message);

        // Act
        await InvokeHandleMessageReceived(sut, args);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessageReceived_PublisherThrowsException_LogsErrorAndContinues()
    {
        // Arrange
        var sut = CreateMqttListener();
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        const int thermostatId = 123;
        const double newTemperature = 22.5;
        var payload = $"{{\"id\":{thermostatId},\"value\":{newTemperature.ToString(CultureInfo.InvariantCulture)}}}";

        _apartmentServiceMock
            .Setup(x => x.ChangeTemperature(thermostatId, newTemperature, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);

        _publisherMock
            .Setup(x => x.Publish(apartment, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Publish failed"));

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("sbm/devices/123/set")
            .WithPayload(payload)
            .Build();

        var args = CreateMessageEventArgs("client1", message);

        // Act
        await InvokeHandleMessageReceived(sut, args);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessageReceived_ValidPayload_LogsDebugAndInfoMessages()
    {
        // Arrange
        var sut = CreateMqttListener();
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        const int thermostatId = 123;
        const double newTemperature = 22.5;
        var payload = $"{{\"id\":{thermostatId},\"value\":{newTemperature.ToString(CultureInfo.InvariantCulture)}}}";

        _apartmentServiceMock
            .Setup(x => x.ChangeTemperature(thermostatId, newTemperature, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("sbm/devices/123/set")
            .WithPayload(payload)
            .Build();

        var args = CreateMessageEventArgs("client1", message);

        // Act
        await InvokeHandleMessageReceived(sut, args);

        // Assert - Verify debug log for received message
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Received message on")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify info log for temperature change
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Changing temperature for thermostat")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CustomSbmTopic_UsesCorrectTopicPattern()
    {
        // Arrange
        var customConfiguration = new MqttConnectorPublisherConfiguration
        {
            SbmTopic = "custom/topic"
        };
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(customConfiguration);

        var sut = CreateMqttListener();
        var cts = new CancellationTokenSource();
        var capturedOptionsList = new List<MqttClientSubscribeOptions>();

        _mqttClientMock
            .Setup(x => x.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .Callback<MqttClientSubscribeOptions, CancellationToken>((options, _) => capturedOptionsList.Add(options))
            .ReturnsAsync(CreateSubscribeResult());

        // Act
        var executeTask = InvokeExecuteAsync(sut, cts.Token);
        await Task.Delay(100);

        cts.Cancel();

        // Wait for the task to complete - it will throw TaskCanceledException from Task.Delay
        try
        {
            await executeTask;
        }
        catch (TaskCanceledException)
        {
            // Expected - the Task.Delay(Timeout.Infinite) throws when cancelled
        }

        // Assert
        Assert.NotEmpty(capturedOptionsList);
        var allTopicFilters = capturedOptionsList.SelectMany(o => o.TopicFilters).ToList();
        var sbmTopicFilter = allTopicFilters.FirstOrDefault(tf => tf.Topic == "custom/topic/devices/+/set");
        Assert.NotNull(sbmTopicFilter);
    }

    [Fact]
    public async Task HandleMessageReceived_ValidPayloadWithFloatingPointTemperature_ProcessesCorrectly()
    {
        // Arrange
        var sut = CreateMqttListener();
        var apartment = ApartmentFakers.ApartmentFaker.Generate();
        const int thermostatId = 456;
        const double newTemperature = 21.75;
        var payload = $"{{\"id\":{thermostatId},\"value\":{newTemperature.ToString(CultureInfo.InvariantCulture)}}}";

        _apartmentServiceMock
            .Setup(x => x.ChangeTemperature(thermostatId, newTemperature, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _apartmentServiceMock
            .Setup(x => x.GetApartmentInfo(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apartment);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("sbm/devices/456/set")
            .WithPayload(payload)
            .Build();

        var args = CreateMessageEventArgs("client1", message);

        // Act
        await InvokeHandleMessageReceived(sut, args);

        // Assert
        _apartmentServiceMock.Verify(
            x => x.ChangeTemperature(thermostatId, 21.75, It.IsAny<CancellationToken>()),
            Times.Once);
        _publisherMock.Verify(
            x => x.Publish(apartment, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private MqttListener CreateMqttListener()
    {
        return new MqttListener(
            _mqttClientMock.Object,
            _apartmentServiceMock.Object,
            _publisherMock.Object,
            _optionsMonitorMock.Object,
            _homeAssistantOptionsMonitorMock.Object,
            _loggerMock.Object);
    }

    private static Task InvokeExecuteAsync(MqttListener listener, CancellationToken cancellationToken)
    {
        // Use reflection to invoke the protected ExecuteAsync method
        var method = typeof(BackgroundService).GetMethod("ExecuteAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("ExecuteAsync method not found");

        return (Task)method.Invoke(listener, new object[] { cancellationToken })!;
    }

    private static async Task InvokeHandleMessageReceived(MqttListener listener,
        MqttApplicationMessageReceivedEventArgs args)
    {
        // Use reflection to invoke the private HandleMessageReceived method
        var method = typeof(MqttListener).GetMethod("HandleMessageReceived",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null) throw new InvalidOperationException("HandleMessageReceived method not found");

        var task = (Task?)method.Invoke(listener, new object[] { args });
        if (task != null) await task;
    }

    private static MqttClientSubscribeResult CreateSubscribeResult()
    {
        // Use a simple approach - mock the SubscribeAsync to just return successfully
        // We don't actually need to create a real MqttClientSubscribeResult for these tests
        var resultType = typeof(MqttClientSubscribeResult);
        var constructors =
            resultType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        // Try to find a constructor and create a minimal valid instance
        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var args = new List<object>();

            try
            {
                foreach (var param in parameters)
                    if (param.ParameterType == typeof(ushort))
                    {
                        args.Add((ushort)1);
                    }
                    else if (param.ParameterType == typeof(IReadOnlyCollection<MqttClientSubscribeResultItem>))
                    {
                        args.Add(Array.Empty<MqttClientSubscribeResultItem>());
                    }
                    else if (param.ParameterType == typeof(string))
                    {
                        args.Add(string.Empty);
                    }
                    else if (param.ParameterType.IsGenericType && param.ParameterType.GetGenericTypeDefinition() ==
                             typeof(IReadOnlyCollection<>))
                    {
                        // Create empty array for generic IReadOnlyCollection types
                        var elementType = param.ParameterType.GetGenericArguments()[0];
                        var array = Array.CreateInstance(elementType, 0);
                        args.Add(array);
                    }
                    else if (param.ParameterType.IsInterface || param.ParameterType.IsAbstract)
                    {
                        // For interfaces/abstract types, try to use null or empty
                        args.Add(null!);
                    }
                    else
                    {
                        try
                        {
                            args.Add(Activator.CreateInstance(param.ParameterType)!);
                        }
                        catch
                        {
                            args.Add(null!);
                        }
                    }

                var result = constructor.Invoke(args.ToArray());
                if (result != null) return (MqttClientSubscribeResult)result;
            }
            catch
            {
                // Try next constructor
            }
        }

        throw new InvalidOperationException("Could not create MqttClientSubscribeResult");
    }

    private static MqttApplicationMessageReceivedEventArgs CreateMessageEventArgs(string clientId,
        MqttApplicationMessage message)
    {
        // Create MqttApplicationMessageReceivedEventArgs using reflection
        var eventType = typeof(MqttApplicationMessageReceivedEventArgs);
        var constructors =
            eventType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var args = new List<object>();

            try
            {
                foreach (var param in parameters)
                    if (param.ParameterType == typeof(string))
                    {
                        args.Add(clientId);
                    }
                    else if (param.ParameterType == typeof(MqttApplicationMessage))
                    {
                        args.Add(message);
                    }
                    else if (param.ParameterType == typeof(CancellationToken))
                    {
                        args.Add(CancellationToken.None);
                    }
                    else if (param.ParameterType.Name.Contains("Func") && param.ParameterType.IsGenericType)
                    {
                        // Create a simple delegate handler
                        args.Add((Func<MqttApplicationMessageReceivedEventArgs, CancellationToken, Task>)((_, _) =>
                            Task.CompletedTask));
                    }
                    else if (param.ParameterType.IsGenericType && param.ParameterType.GetGenericTypeDefinition() ==
                             typeof(IReadOnlyCollection<>))
                    {
                        var elementType = param.ParameterType.GetGenericArguments()[0];
                        var array = Array.CreateInstance(elementType, 0);
                        args.Add(array);
                    }
                    else if (param.ParameterType.IsInterface || param.ParameterType.IsAbstract)
                    {
                        args.Add(null!);
                    }
                    else
                    {
                        try
                        {
                            args.Add(Activator.CreateInstance(param.ParameterType)!);
                        }
                        catch
                        {
                            args.Add(null!);
                        }
                    }

                var result = constructor.Invoke(args.ToArray());
                if (result != null) return (MqttApplicationMessageReceivedEventArgs)result;
            }
            catch
            {
                // Try next constructor
            }
        }

        throw new InvalidOperationException("Could not create MqttApplicationMessageReceivedEventArgs");
    }
}