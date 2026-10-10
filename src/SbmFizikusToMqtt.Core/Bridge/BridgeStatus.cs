namespace SbmFizikusToMqtt.Core.Bridge;

/// <summary>Bridge health state, written by the bridge and the MQTT adapter and read by the health checks.</summary>
public sealed class BridgeStatus(TimeProvider timeProvider)
{
    private readonly Lock _lock = new();
    private DateTimeOffset? _lastSuccessfulPoll;
    private bool _mqttConnected;

    public bool IsMqttConnected
    {
        get
        {
            lock (_lock)
                return _mqttConnected;
        }
    }

    public DateTimeOffset? LastSuccessfulPoll
    {
        get
        {
            lock (_lock)
                return _lastSuccessfulPoll;
        }
    }

    public void MarkMqttConnected()
    {
        lock (_lock)
            _mqttConnected = true;
    }

    public void MarkMqttDisconnected()
    {
        lock (_lock)
            _mqttConnected = false;
    }

    public void MarkPollSucceeded()
    {
        lock (_lock)
            _lastSuccessfulPoll = timeProvider.GetUtcNow();
    }
}