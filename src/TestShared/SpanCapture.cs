using System.Diagnostics;

namespace SbmFizikusToMqtt.TestShared;

/// <summary>Records the spans an action produces, for asserting on tracing.</summary>
internal static class SpanCapture
{
    /// <summary>
    ///     Runs the action under a dedicated parent span and returns the spans of the given sources that belong to that
    ///     trace, in the order they stopped. Spans from tests running in parallel are ignored, and so are exceptions
    ///     thrown by the action.
    /// </summary>
    public static async Task<List<Activity>> CaptureAsync(Func<Task> action, params string[] sourceNames)
    {
        var stopped = new List<Activity>();
        using var listener = new ActivityListener();
        listener.ShouldListenTo = source => sourceNames.Contains(source.Name);
        listener.Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = activity =>
        {
            lock (stopped)
                stopped.Add(activity);
        };
        ActivitySource.AddActivityListener(listener);

        using var parent = new Activity("test").SetIdFormat(ActivityIdFormat.W3C).Start();
        await Record.ExceptionAsync(action);
        parent.Stop();

        lock (stopped)
            return stopped.Where(x => x.TraceId == parent.TraceId).ToList();
    }
}