using Microsoft.Extensions.Logging;

namespace SbmFizikusToMqtt.Core.Tests.TestSupport;

/// <summary>Records every log entry with its rendered message and structured values.</summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var values = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
        Entries.Add(new LogEntry(logLevel, formatter(state, exception),
            values.ToDictionary(x => x.Key, x => x.Value)));
    }

    public sealed record LogEntry(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Values);
}