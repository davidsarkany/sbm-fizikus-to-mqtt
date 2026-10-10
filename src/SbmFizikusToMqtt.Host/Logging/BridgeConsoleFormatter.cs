using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace SbmFizikusToMqtt.Host.Logging;

/// <summary>
///     One line per entry, e.g. <c>[14:03:27 INF] Connected to the MQTT broker &lt;s:SbmFizikusToMqtt...&gt;</c>, with
///     the exception, if any, on the following lines. The timestamp is local time (the container's <c>TZ</c>).
/// </summary>
internal sealed class BridgeConsoleFormatter(TimeProvider timeProvider) : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "bridge";

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (string.IsNullOrEmpty(message) && logEntry.Exception is null)
            return;

        Span<char> timestamp = stackalloc char[8];
        timeProvider.GetLocalNow().TryFormat(timestamp, out var written, "HH:mm:ss", CultureInfo.InvariantCulture);

        textWriter.Write('[');
        textWriter.Write(timestamp[..written]);
        textWriter.Write(' ');
        textWriter.Write(Abbreviate(logEntry.LogLevel));
        textWriter.Write("] ");
        textWriter.Write(message);
        textWriter.Write(" <s:");
        textWriter.Write(logEntry.Category);
        textWriter.Write('>');
        textWriter.Write(Environment.NewLine);

        if (logEntry.Exception is not null)
        {
            textWriter.Write(logEntry.Exception.ToString());
            textWriter.Write(Environment.NewLine);
        }
    }

    private static string Abbreviate(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => level.ToString().ToUpperInvariant()
        };
    }
}