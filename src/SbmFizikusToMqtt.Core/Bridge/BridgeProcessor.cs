using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SbmFizikusToMqtt.Core.Discovery;
using SbmFizikusToMqtt.Core.Domain;
using SbmFizikusToMqtt.Core.Options;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Core.Publishing;

namespace SbmFizikusToMqtt.Core.Bridge;

/// <summary>
///     The bridge's orchestration. It handles one <see cref="BridgeCommand" /> at a time; the caller must not invoke
///     <see cref="HandleAsync" /> concurrently. Failures are logged and never escape, except cancellation.
/// </summary>
public sealed class BridgeProcessor
{
    private const string HomeAssistantOnlinePayload = "online";

    private readonly DiscoveryBuilder _discoveryBuilder;
    private readonly ILogger<BridgeProcessor> _logger;
    private readonly IMqttGateway _mqttGateway;
    private readonly ISbmGateway _sbmGateway;
    private readonly StatePayloadMapper _statePayloadMapper;
    private readonly BridgeStatus _status;
    private readonly TimeProvider _timeProvider;
    private readonly MqttTopics _topics;

    private bool _discoveryPending = true;
    private Apartment? _lastApartment;
    private string? _publishedDiscoveryKey;
    private HashSet<int> _publishedThermostatIds = [];

    public BridgeProcessor(
        ISbmGateway sbmGateway,
        IMqttGateway mqttGateway,
        BridgeStatus status,
        MqttTopics topics,
        DiscoveryOptions discoveryOptions,
        TimeProvider timeProvider,
        ILogger<BridgeProcessor> logger)
    {
        _sbmGateway = sbmGateway;
        _mqttGateway = mqttGateway;
        _status = status;
        _topics = topics;
        _timeProvider = timeProvider;
        _logger = logger;
        _statePayloadMapper = new StatePayloadMapper(topics);
        _discoveryBuilder = new DiscoveryBuilder(topics, discoveryOptions);
    }

    public async Task HandleAsync(BridgeCommand command, CancellationToken cancellationToken)
    {
        // One trace per command; the SBM and discovery spans it causes become its children.
        var commandName = GetCommandName(command);
        using var activity = BridgeTelemetry.ActivitySource.StartActivity($"bridge {commandName}");
        activity?.SetTag(BridgeTelemetry.CommandTag, commandName);
        if (command is MqttMessageReceived received)
            activity?.SetTag(BridgeTelemetry.TopicTag, received.Topic);

        try
        {
            switch (command)
            {
                case PollRequested:
                    await RefreshAsync(commandName, cancellationToken);
                    break;
                case MqttConnected:
                    // Retained discovery may have been lost (e.g. a broker without persistence restarted).
                    _discoveryPending = true;
                    await RefreshAsync(commandName, cancellationToken);
                    break;
                case MqttMessageReceived message:
                    await HandleMessageAsync(message, commandName, activity, cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            BridgeTelemetry.RecordFailure(activity, ex);
            _logger.LogError(ex, "Failed to handle {Command}", command.GetType().Name);
        }
    }

    private string GetCommandName(BridgeCommand command)
    {
        return command switch
        {
            PollRequested => "poll",
            MqttConnected => "mqtt_connected",
            MqttMessageReceived message when message.Topic == _topics.HomeAssistantStatus => "homeassistant_status",
            MqttMessageReceived message when _topics.TryParseDeviceSet(message.Topic, out _) => "set_temperature",
            _ => "mqtt_message"
        };
    }

    private async Task HandleMessageAsync(MqttMessageReceived message, string commandName, Activity? activity,
        CancellationToken cancellationToken)
    {
        if (message.Topic == _topics.HomeAssistantStatus)
        {
            if (message.Payload == HomeAssistantOnlinePayload)
            {
                _logger.LogInformation("Home Assistant came online, re-publishing state and discovery");
                _discoveryPending = true;
                await RefreshAsync(commandName, cancellationToken);
            }

            return;
        }

        if (!_topics.TryParseDeviceSet(message.Topic, out var thermostatId))
        {
            _logger.LogDebug("Ignoring message on unexpected topic {Topic}", message.Topic);
            return;
        }

        // Rejections are invalid input, not bridge failures: they are tagged on the span but not marked as errors.
        // A retained command is an old one replayed on every (re)subscribe; applying it would override changes made
        // since then.
        if (message.Retained)
        {
            activity?.SetTag(BridgeTelemetry.RejectedTag, "retained command");
            _logger.LogWarning("Ignoring retained temperature command on {Topic}; clear it with an empty retained " +
                               "message. Payload: {Payload}", message.Topic, message.Payload);
            return;
        }

        if (!TemperatureCommand.TryParse(thermostatId, message.Payload, out var command, out var error))
        {
            activity?.SetTag(BridgeTelemetry.RejectedTag, error);
            _logger.LogWarning("Rejected temperature command on {Topic}: {Error}. Payload: {Payload}",
                message.Topic, error, message.Payload);
            return;
        }

        _lastApartment ??= await _sbmGateway.GetApartmentAsync(cancellationToken);
        if (_lastApartment.Thermostats.All(x => x.Id != command.ThermostatId))
        {
            activity?.SetTag(BridgeTelemetry.RejectedTag, $"unknown thermostat {command.ThermostatId}");
            _logger.LogWarning("Rejected temperature command for unknown thermostat {ThermostatId}",
                command.ThermostatId);
            return;
        }

        _logger.LogInformation("Setting thermostat {ThermostatId} target temperature to {Temperature}",
            command.ThermostatId, command.Temperature);
        await _sbmGateway.SetTargetTemperatureAsync(command.ThermostatId, command.Temperature, cancellationToken);
        await RefreshAsync(commandName, cancellationToken);
    }

    private async Task RefreshAsync(string trigger, CancellationToken cancellationToken)
    {
        var apartment = await _sbmGateway.GetApartmentAsync(cancellationToken);
        _lastApartment = apartment;

        foreach (var message in _statePayloadMapper.Map(apartment))
            await _mqttGateway.PublishAsync(message, cancellationToken);

        _status.MarkPollSucceeded();
        LogRefresh(trigger, apartment);

        var discoveryKey = CreateDiscoveryKey(apartment);
        if (_discoveryPending || discoveryKey != _publishedDiscoveryKey)
        {
            await PublishDiscoveryAsync(apartment, cancellationToken);
            await RemoveMissingThermostatsAsync(apartment, cancellationToken);
            _publishedDiscoveryKey = discoveryKey;
            _discoveryPending = false;
        }
    }

    /// <summary>
    ///     One line per successful refresh with the state SBM reported. Timings are left to the trace. A growing data
    ///     age means SBM answers with old data, e.g. while the building's relay module is offline.
    /// </summary>
    private void LogRefresh(string trigger, Apartment apartment)
    {
        var dataAge = _timeProvider.GetUtcNow() - apartment.LastUpdate;
        _logger.LogInformation(
            "Refreshed state ({Trigger}): {ThermostatCount} thermostats, {ActiveCount} calling, {SystemMode}, " +
            "SBM {SbmConnection}, data age {DataAgeSeconds} s",
            trigger,
            apartment.Thermostats.Count,
            apartment.Thermostats.Count(x => x.Active),
            apartment.SystemMode.ToWireValue(),
            apartment.IsConnected ? "connected" : "disconnected",
            (long)dataAge.TotalSeconds);
    }

    /// <summary>
    ///     Clears the retained discovery config and state of thermostats SBM no longer reports; otherwise Home
    ///     Assistant would keep them as available entities with frozen values. An empty list is treated as a
    ///     transient SBM glitch and removes nothing. Only thermostats seen since the bridge started are tracked.
    /// </summary>
    private async Task RemoveMissingThermostatsAsync(Apartment apartment, CancellationToken cancellationToken)
    {
        if (apartment.Thermostats.Count == 0)
            return;

        var currentIds = apartment.Thermostats.Select(x => x.Id).ToHashSet();
        foreach (var thermostatId in _publishedThermostatIds.Where(x => !currentIds.Contains(x)).Order())
        {
            await _mqttGateway.PublishAsync(CreateClearMessage(_topics.ThermostatDiscovery(thermostatId)),
                cancellationToken);
            await _mqttGateway.PublishAsync(CreateClearMessage(_topics.Device(thermostatId)), cancellationToken);
            _logger.LogInformation("Thermostat {ThermostatId} is no longer reported by SBM, removed it from Home " +
                                   "Assistant", thermostatId);
        }

        _publishedThermostatIds = currentIds;
    }

    /// <summary>An empty retained message deletes the retained message stored on the topic.</summary>
    private static MqttMessage CreateClearMessage(string topic)
    {
        return new MqttMessage { Topic = topic, Payload = string.Empty, Retain = true, Qos = MqttQos.AtLeastOnce };
    }

    /// <summary>
    ///     Publishes the device configs. Switched-off entities are removed through the configs themselves; v2 configs
    ///     are not migrated and must be cleaned up before upgrading (see README).
    /// </summary>
    private async Task PublishDiscoveryAsync(Apartment apartment, CancellationToken cancellationToken)
    {
        using var activity = BridgeTelemetry.ActivitySource.StartActivity("bridge publish_discovery");
        try
        {
            var devices = _discoveryBuilder.Build(apartment);
            activity?.SetTag("bridge.discovery.devices", devices.Count);

            foreach (var device in devices)
                await _mqttGateway.PublishAsync(_discoveryBuilder.ToDeviceMessage(device), cancellationToken);

            _logger.LogInformation("Published Home Assistant discovery for {DeviceCount} devices", devices.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            BridgeTelemetry.RecordFailure(activity, ex);
            throw;
        }
    }

    /// <summary>Discovery depends on the thermostat ids and names; any change requires re-publishing it.</summary>
    private static string CreateDiscoveryKey(Apartment apartment)
    {
        return string.Join('|', apartment.Thermostats.OrderBy(x => x.Id).Select(x => $"{x.Id}:{x.Name}"));
    }
}