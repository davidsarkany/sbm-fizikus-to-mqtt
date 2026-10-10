using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using SbmFizikusToMqtt.Core.Bridge;
using SbmFizikusToMqtt.Core.Ports;
using SbmFizikusToMqtt.Core.Publishing;

namespace SbmFizikusToMqtt.Infrastructure.Mqtt;

/// <summary>
///     Owns the MQTT connection. It connects with a retained <c>offline</c> last will, keeps reconnecting with
///     exponential backoff, and after every connect publishes <c>online</c>, re-subscribes and enqueues
///     <see cref="MqttConnected" />. Incoming messages are forwarded to the <see cref="BridgeCommandQueue" />.
/// </summary>
internal sealed class MqttGateway : BackgroundService, IMqttGateway
{
    private readonly IMqttClient _client;
    private readonly ILogger<MqttGateway> _logger;
    private readonly MqttOptions _options;
    private readonly BridgeCommandQueue _queue;
    private readonly BridgeStatus _status;
    private readonly TimeProvider _timeProvider;
    private readonly MqttTopics _topics;

    private TaskCompletionSource _connectionLost = NewConnectionLostSource();
    private volatile bool _resetting;
    private volatile bool _stopping;

    public MqttGateway(
        IOptions<MqttOptions> options,
        MqttTopics topics,
        BridgeStatus status,
        BridgeCommandQueue queue,
        TimeProvider timeProvider,
        ILogger<MqttGateway> logger)
    {
        _options = options.Value;
        _topics = topics;
        _status = status;
        _queue = queue;
        _timeProvider = timeProvider;
        _logger = logger;
        ClientOptions = BuildClientOptions(_options, topics);

        _client = new MqttClientFactory().CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;
    }

    internal MqttClientOptions ClientOptions { get; }

    public async Task PublishAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        if (!_client.IsConnected)
            throw new InvalidOperationException($"Cannot publish to {message.Topic}: MQTT is not connected");

        var result = await _client.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(message.Topic)
            .WithPayload(message.Payload)
            .WithRetainFlag(message.Retain)
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)message.Qos)
            .Build(), cancellationToken);

        if (!result.IsSuccess)
            throw new InvalidOperationException($"Publishing to {message.Topic} failed: {result.ReasonCode}");
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping = true;
        await base.StopAsync(cancellationToken);

        if (_client.IsConnected)
        {
            try
            {
                // A clean disconnect suppresses the last will, so announce the shutdown explicitly.
                await PublishAsync(new MqttMessage
                {
                    Topic = _topics.BridgeState,
                    Payload = BridgeStatePayloads.Offline,
                    Retain = true,
                    Qos = MqttQos.AtLeastOnce
                }, cancellationToken);
                await _client.DisconnectAsync(new MqttClientDisconnectOptionsBuilder()
                    .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                    .Build(), cancellationToken);
                _logger.LogInformation("Disconnected from MQTT broker");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to disconnect cleanly from the MQTT broker");
            }
        }

        _status.MarkMqttDisconnected();
    }

    public override void Dispose()
    {
        _client.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = ReconnectBackoff.Initial;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connectionLost = await ConnectAsync(stoppingToken);
                delay = ReconnectBackoff.Initial;
                await connectionLost.WaitAsync(stoppingToken);

                // Also set by OnDisconnectedAsync, but a disconnect racing ConnectAsync can run before it marks the
                // connection as up; this loop is sequenced after both.
                _status.MarkMqttDisconnected();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _status.MarkMqttDisconnected();
                _logger.LogWarning(ex, "Cannot connect to MQTT broker {Host}:{Port}, retrying in {Delay}",
                    _options.Host, _options.Port, delay);
                await DisconnectQuietlyAsync();

                try
                {
                    await Task.Delay(delay, _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                delay = ReconnectBackoff.Next(delay);
            }
        }
    }

    private async Task<Task> ConnectAsync(CancellationToken cancellationToken)
    {
        _connectionLost = NewConnectionLostSource();
        var connectionLost = _connectionLost.Task;

        _logger.LogInformation("Connecting to MQTT broker {Host}:{Port}", _options.Host, _options.Port);
        await _client.ConnectAsync(ClientOptions, cancellationToken);

        await PublishAsync(new MqttMessage
        {
            Topic = _topics.BridgeState,
            Payload = BridgeStatePayloads.Online,
            Retain = true,
            Qos = MqttQos.AtLeastOnce
        }, cancellationToken);

        await _client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(_topics.DeviceSetFilter, MqttQualityOfServiceLevel.AtLeastOnce)
            .WithTopicFilter(_topics.HomeAssistantStatus, MqttQualityOfServiceLevel.AtLeastOnce)
            .Build(), cancellationToken);

        _status.MarkMqttConnected();
        _queue.Enqueue(new MqttConnected());
        _logger.LogInformation("Connected to MQTT broker and subscribed to {SetTopic} and {StatusTopic}",
            _topics.DeviceSetFilter, _topics.HomeAssistantStatus);
        return connectionLost;
    }

    private async Task DisconnectQuietlyAsync()
    {
        _resetting = true;
        try
        {
            if (_client.IsConnected)
                await _client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ignoring error while resetting the MQTT connection");
        }
        finally
        {
            _resetting = false;
        }
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        _status.MarkMqttDisconnected();

        // A reset after a failed connect is our own disconnect and already logged as such.
        if (_stopping || _resetting || !args.ClientWasConnected)
            return Task.CompletedTask;

        _logger.LogWarning(args.Exception, "MQTT connection lost ({Reason}), reconnecting", args.Reason);
        _connectionLost.TrySetResult();
        return Task.CompletedTask;
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var message = args.ApplicationMessage;

        // MQTTnet returns null for an empty payload.
        _queue.Enqueue(new MqttMessageReceived(message.Topic, message.ConvertPayloadToString() ?? string.Empty,
            message.Retain));
        return Task.CompletedTask;
    }

    private static MqttClientOptions BuildClientOptions(MqttOptions options, MqttTopics topics)
    {
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(options.Host, options.Port)
            .WithClientId(options.ClientId)
            .WithCleanSession()
            .WithWillTopic(topics.BridgeState)
            .WithWillPayload(BridgeStatePayloads.Offline)
            .WithWillRetain()
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);

        if (!string.IsNullOrEmpty(options.Username))
            builder.WithCredentials(options.Username, options.Password);

        return builder.Build();
    }

    private static TaskCompletionSource NewConnectionLostSource()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}