namespace SbmFizikusToMqtt.Core.Ports;

public enum MqttQos
{
    AtMostOnce = 0,
    AtLeastOnce = 1
}

public sealed record MqttMessage
{
    public required string Topic { get; init; }
    public required string Payload { get; init; }
    public bool Retain { get; init; }
    public MqttQos Qos { get; init; } = MqttQos.AtMostOnce;
}