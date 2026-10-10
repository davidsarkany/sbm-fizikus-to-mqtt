using System.ComponentModel.DataAnnotations;

namespace SbmFizikusToMqtt.Infrastructure.Mqtt;

public sealed record MqttOptions
{
    [Required] public required string Host { get; init; }

    [Range(1, 65535)] public int Port { get; init; } = 1883;

    /// <summary>Optional; anonymous connection when empty.</summary>
    public string? Username { get; init; }

    public string? Password { get; init; }

    [Required] public string ClientId { get; init; } = "sbm-fizikus-mqtt";
}