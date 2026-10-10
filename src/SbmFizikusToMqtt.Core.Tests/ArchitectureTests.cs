using System.Reflection;

namespace SbmFizikusToMqtt.Core.Tests;

/// <summary>
///     Guards the Core boundary: application logic must not depend on I/O, hosting or transport libraries.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly string[] AllowedReferencePrefixes =
    [
        "System.",
        "Microsoft.Extensions.Logging.Abstractions"
    ];

    [Fact]
    public void CoreAssembly_ReferencedAssemblies_OnlyAllowedDependencies()
    {
        // Arrange
        var coreAssembly = Assembly.Load("SbmFizikusToMqtt.Core");

        // Act
        var disallowed = coreAssembly.GetReferencedAssemblies()
            .Select(x => x.Name!)
            .Where(name => name != "System" && name != "netstandard")
            .Where(name => !AllowedReferencePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        // Assert
        Assert.Empty(disallowed);
    }
}