using System.ComponentModel.DataAnnotations;

namespace SbmFizikusToMqtt.Infrastructure.Sbm;

public sealed record SbmOptions
{
    [Required] public required string Username { get; init; }

    [Required] public required string Password { get; init; }

    [Required][Url] public required string BaseUrl { get; init; }

    /// <summary>Fetch outdoor temperature and humidity on every poll.</summary>
    public bool OutdoorWeatherEnabled { get; init; }
}