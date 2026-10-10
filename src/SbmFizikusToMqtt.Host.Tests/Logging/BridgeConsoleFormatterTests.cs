using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SbmFizikusToMqtt.Host.Logging;

namespace SbmFizikusToMqtt.Host.Tests.Logging;

/// <summary>The console log line format.</summary>
public sealed class BridgeConsoleFormatterTests
{
    private const string Category = "SbmFizikusToMqtt.Core.Bridge.BridgeProcessor";

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 12, 14, 3, 27, TimeSpan.Zero));

    public BridgeConsoleFormatterTests()
    {
        _time.SetLocalTimeZone(TimeZoneInfo.Utc);
    }

    [Fact]
    public void Write_Message_WritesTimestampLevelMessageAndCategory()
    {
        // Arrange
        var entry = Entry(LogLevel.Information, "Connected to the MQTT broker");

        // Act
        var line = Format(entry);

        // Assert
        Assert.Equal($"[14:03:27 INF] Connected to the MQTT broker <s:{Category}>{Environment.NewLine}", line);
    }

    [Fact]
    public void Write_Exception_WritesExceptionAfterMessageLine()
    {
        // Arrange
        var exception = new InvalidOperationException("boom");
        var entry = Entry(LogLevel.Error, "Poll failed", exception);

        // Act
        var line = Format(entry);

        // Assert
        Assert.Equal($"[14:03:27 ERR] Poll failed <s:{Category}>{Environment.NewLine}{exception}{Environment.NewLine}",
            line);
    }

    [Fact]
    public void Write_EmptyMessageWithoutException_WritesNothing()
    {
        // Arrange
        var entry = Entry(LogLevel.Information, "");

        // Act
        var line = Format(entry);

        // Assert
        Assert.Empty(line);
    }

    [Theory]
    [InlineData(LogLevel.Trace, "TRC")]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Information, "INF")]
    [InlineData(LogLevel.Warning, "WRN")]
    [InlineData(LogLevel.Error, "ERR")]
    [InlineData(LogLevel.Critical, "CRT")]
    public void Write_Level_WritesThreeLetterAbbreviation(LogLevel level, string expected)
    {
        // Arrange
        var entry = Entry(level, "message");

        // Act
        var line = Format(entry);

        // Assert
        Assert.StartsWith($"[14:03:27 {expected}] ", line);
    }

    private static LogEntry<string> Entry(LogLevel level, string message, Exception? exception = null)
    {
        return new LogEntry<string>(level, Category, new EventId(0), message, exception, (state, _) => state);
    }

    private string Format(LogEntry<string> entry)
    {
        using var writer = new StringWriter();
        new BridgeConsoleFormatter(_time).Write(entry, null, writer);
        return writer.ToString();
    }
}