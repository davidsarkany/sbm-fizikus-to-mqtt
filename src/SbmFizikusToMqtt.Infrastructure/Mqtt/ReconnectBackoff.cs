namespace SbmFizikusToMqtt.Infrastructure.Mqtt;

/// <summary>Exponential reconnect delay: 1 s, 2 s, 4 s … capped at 60 s.</summary>
internal static class ReconnectBackoff
{
    public static readonly TimeSpan Initial = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan Maximum = TimeSpan.FromSeconds(60);

    public static TimeSpan Next(TimeSpan current)
    {
        var doubled = current * 2;
        return doubled > Maximum ? Maximum : doubled;
    }
}