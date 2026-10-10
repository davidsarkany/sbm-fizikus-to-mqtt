using System.Diagnostics;

namespace SbmFizikusToMqtt.Infrastructure.Sbm;

/// <summary>
///     Tracing for SBM calls. Every SBM operation goes to the same <c>PUT /frontend</c> endpoint, so each call gets a
///     span named after its operation (e.g. <c>SBM get_apartment_data</c>), and the HTTP span becomes its child.
/// </summary>
public static class SbmTelemetry
{
    /// <summary>Register this source with the tracer provider to export the operation spans.</summary>
    public const string ActivitySourceName = "SbmFizikusToMqtt.Sbm";

    internal const string OperationTag = "sbm.operation";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}