namespace SbmFizikusToMqtt.Infrastructure.Tests.TestSupport;

/// <summary>Polls an asynchronous condition until it holds or the timeout elapses.</summary>
internal static class Eventually
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    public static async Task TrueAsync(Func<Task<bool>> condition, string because, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(50);
        }

        Assert.Fail($"Timed out waiting until {because}");
    }

    public static Task TrueAsync(Func<bool> condition, string because, TimeSpan? timeout = null)
    {
        return TrueAsync(() => Task.FromResult(condition()), because, timeout);
    }
}