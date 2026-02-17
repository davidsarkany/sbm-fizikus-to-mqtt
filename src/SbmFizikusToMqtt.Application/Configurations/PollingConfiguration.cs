using System.ComponentModel.DataAnnotations;

namespace SbmFizikusToMqtt.Application.Configurations;

public sealed record PollingConfiguration
{
    [Required] public required string PollingCronExpression { get; init; }
}