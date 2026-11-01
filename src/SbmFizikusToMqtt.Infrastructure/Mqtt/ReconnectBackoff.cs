namespace SbmFizikusToMqtt.Infrastructure.Mqtt;

/// <summary>
///     Exponential reconnect delay: 1 s, 2 s, 4 s … capped at 60 s. It also applies after a lost connection, and only
///     a connection that stayed up for <see cref="StableConnection" /> starts over from <see cref="Initial" />.
/// </summary>
internal static class ReconnectBackoff
{
    public static readonly TimeSpan Initial = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan Maximum = TimeSpan.FromSeconds(60);

    /// <summary>
    ///     A connection lost sooner than this counts as a failed attempt, e.g. when another client with the same
    ///     client id takes the session over; reconnecting at once would make the two clients kick each other off.
    /// </summary>
    public static readonly TimeSpan StableConnection = TimeSpan.FromSeconds(60);

    public static TimeSpan Next(TimeSpan current)
    {
        var doubled = current * 2;
        return doubled > Maximum ? Maximum : doubled;
    }
}