using System.ComponentModel.DataAnnotations;

namespace SbmFizikusToMqtt.MqttConnector.Configurations;

public sealed record MqttServerConfiguration
{
    [Required] public required string Host { get; init; }

    [Required] public required int Port { get; init; }

    [Required] public required string Username { get; init; }

    [Required] public required string Password { get; init; }

    [Required] public required string ClientId { get; init; }
}