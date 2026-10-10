using System.Reflection;

namespace SbmFizikusToMqtt.Host.Configuration;

internal static class HostVersion
{
    /// <summary>The application version, e.g. <c>3.0.0</c>, without the SDK's source revision suffix.</summary>
    public static string Current { get; } = Read();

    private static string Read()
    {
        var version = typeof(HostVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? version : version[..plus];
    }
}