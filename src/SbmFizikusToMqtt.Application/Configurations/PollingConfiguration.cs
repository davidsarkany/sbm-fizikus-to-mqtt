using System.ComponentModel.DataAnnotations;

namespace SbmFizikusToMqtt.Application.Configurations;

public sealed record PollingConfiguration
{
    [Range(1, int.MaxValue)] public int PollingIntervalSeconds { get; init; }
}