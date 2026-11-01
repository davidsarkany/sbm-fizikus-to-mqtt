namespace SbmFizikusToMqtt.Core.Options;

/// <summary>When the published state counts as stale and Home Assistant must show the entities as unavailable.</summary>
public sealed record AvailabilityOptions
{
    /// <summary>
    ///     Time without a successful SBM refresh after which <c>apartment_info</c> is republished with
    ///     <c>connected: false</c>, so the entities stop showing frozen values as current.
    /// </summary>
    public required TimeSpan StaleAfter { get; init; }
}