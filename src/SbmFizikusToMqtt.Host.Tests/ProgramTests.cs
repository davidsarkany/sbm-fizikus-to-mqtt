namespace SbmFizikusToMqtt.Host.Tests;

/// <summary>
///     Runs the real entry point. WebApplicationFactory starts the host itself, so only this reaches the startup
///     failure path in <c>Program</c>.
/// </summary>
public sealed class ProgramTests
{
    [Fact]
    public void Main_InvalidConfiguration_LogsCriticalAndReturnsOne()
    {
        // Arrange
        string[] args =
        [
            "--SbmConnector:Username=",
            "--SbmConnector:Password=secret",
            "--SbmConnector:BaseUrl=https://sbm.example.com",
            "--MqttConnector:MqttServer:Host=mqtt.local"
        ];
        var entryPoint = typeof(Program).Assembly.EntryPoint!;
        var originalOut = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);

        // Act
        int exitCode;
        try
        {
            exitCode = (int)entryPoint.Invoke(null, [args])!;
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        // Assert
        Assert.Equal(1, exitCode);
        var log = output.ToString();
        Assert.Contains("CRT] SBM Fizikus to MQTT terminated unexpectedly", log, StringComparison.Ordinal);
        Assert.Contains("OptionsValidationException", log, StringComparison.Ordinal);
        Assert.Contains("Username: ", log, StringComparison.Ordinal);
    }
}