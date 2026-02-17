using System.ComponentModel.DataAnnotations;

namespace SbmFizikusToMqtt.MqttConnector.Configurations;

public sealed record MqttConnectorPublisherConfiguration
{
    [Required] public required string SbmTopic { get; init; }
}