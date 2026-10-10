using System.Diagnostics;

namespace SbmFizikusToMqtt.Core.Bridge;

/// <summary>
///     Tracing for the bridge: every handled command is one trace (e.g. <c>bridge poll</c>) that contains the SBM
///     calls and the discovery publication it caused, so a whole poll can be followed in one waterfall.
/// </summary>
public static class BridgeTelemetry
{
    /// <summary>Register this source with the tracer provider to export the bridge spans.</summary>
    public const string ActivitySourceName = "SbmFizikusToMqtt.Bridge";

    internal const string CommandTag = "bridge.command";
    internal const string RejectedTag = "bridge.rejected";
    internal const string TopicTag = "mqtt.topic";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    internal static void RecordFailure(Activity? activity, Exception exception)
    {
        if (activity is null)
            return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddException(exception);
    }
}