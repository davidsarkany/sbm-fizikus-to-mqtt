using System.ComponentModel.DataAnnotations;

namespace SbmFizikusToMqtt.SbmConnector.Configurations;

public sealed record SbmConfiguration
{
    [Required] public required string Username { get; init; }

    [Required] public required string Password { get; init; }

    [Required] [Url] public required string BaseUrl { get; init; }
}