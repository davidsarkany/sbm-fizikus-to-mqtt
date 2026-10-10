using System.Runtime.CompilerServices;

namespace SbmFizikusToMqtt.TestShared;

/// <summary>
///     Minimal golden-file assertion. The expected text lives next to the test source as
///     <c>{TestClass}.{TestMethod}.verified.txt</c>. On a mismatch the actual text is written to
///     <c>.received.txt</c> and the assertion fails; review the diff and rename it to accept, or run the tests with
///     <c>SNAPSHOT_ACCEPT=1</c> to overwrite the verified files.
/// </summary>
internal static class Snapshot
{
    private const string AcceptVariable = "SNAPSHOT_ACCEPT";

    public static void Match(string actual, [CallerFilePath] string sourceFile = "",
        [CallerMemberName] string testName = "")
    {
        var directory = Path.GetDirectoryName(sourceFile)!;
        var baseName = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(sourceFile)}.{testName}");
        var verifiedPath = $"{baseName}.verified.txt";
        var receivedPath = $"{baseName}.received.txt";

        var normalized = Normalize(actual);
        var expected = File.Exists(verifiedPath) ? Normalize(File.ReadAllText(verifiedPath)) : null;

        if (expected == normalized)
        {
            File.Delete(receivedPath);
            return;
        }

        if (Environment.GetEnvironmentVariable(AcceptVariable) == "1")
        {
            File.WriteAllText(verifiedPath, normalized);
            File.Delete(receivedPath);
            return;
        }

        File.WriteAllText(receivedPath, normalized);
        Assert.Fail(expected is null
            ? $"No verified snapshot yet. Review {receivedPath} and rename it to .verified.txt to accept it."
            : $"Snapshot mismatch. Compare {receivedPath} with {verifiedPath}; rename it to accept the change.");
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n') + "\n";
    }
}