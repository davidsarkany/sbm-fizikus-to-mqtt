namespace SbmFizikusToMqtt.MqttConnector.Domain;

public sealed record MqttMessage
{
    public required string Topic { get; init; }
    public required string Payload { get; init; }
}