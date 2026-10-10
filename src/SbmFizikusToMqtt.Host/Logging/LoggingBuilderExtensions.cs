using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Console;

namespace SbmFizikusToMqtt.Host.Logging;

public static class LoggingBuilderExtensions
{
    /// <summary>
    ///     Writes logs to the console with <see cref="BridgeConsoleFormatter" />. Levels come from the standard
    ///     <c>Logging:LogLevel</c> section (environment: <c>Logging__LogLevel__*</c>) and apply to every provider,
    ///     OpenTelemetry included.
    /// </summary>
    public static ILoggingBuilder AddBridgeConsole(this ILoggingBuilder logging)
    {
        logging.Services.TryAddSingleton(TimeProvider.System);
        logging.AddConsole(options => options.FormatterName = BridgeConsoleFormatter.FormatterName);
        // Registered directly: AddConsoleFormatter binds formatter options by reflection, which Native AOT forbids.
        logging.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ConsoleFormatter, BridgeConsoleFormatter>());
        return logging;
    }
}