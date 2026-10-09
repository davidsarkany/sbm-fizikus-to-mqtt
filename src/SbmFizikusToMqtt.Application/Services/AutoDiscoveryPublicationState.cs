namespace SbmFizikusToMqtt.Application.Services;

/// <summary>
///     Tracks whether Home Assistant auto-discovery has been published successfully at least once.
///     Until the first successful publication (for example after a transient SBM API outage during
///     startup or while handling the Home Assistant birth message) the polling background service
///     retries it on every successful poll.
/// </summary>
internal interface IAutoDiscoveryPublicationState
{
    bool HasPublishedDiscovery { get; }

    void MarkDiscoveryPublished();
}

internal sealed class AutoDiscoveryPublicationState : IAutoDiscoveryPublicationState
{
    private int _hasPublishedDiscovery;

    public bool HasPublishedDiscovery => Volatile.Read(ref _hasPublishedDiscovery) == 1;

    public void MarkDiscoveryPublished()
    {
        Volatile.Write(ref _hasPublishedDiscovery, 1);
    }
}