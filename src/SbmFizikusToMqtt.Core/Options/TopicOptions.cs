namespace SbmFizikusToMqtt.Core.Options;

public sealed record TopicOptions
{
    public required string SbmTopic { get; init; }
    public required string HomeAssistantTopic { get; init; }
}