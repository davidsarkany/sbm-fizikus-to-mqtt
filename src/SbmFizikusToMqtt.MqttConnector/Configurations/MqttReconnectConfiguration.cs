namespace SbmFizikusToMqtt.MqttConnector.Configurations;

public sealed record MqttReconnectConfiguration
{
    public required int MaxReconnectAttempts { get; init; }

    public required int InitialDelaySeconds { get; init; }

    public required int MaxDelaySeconds { get; init; }
}