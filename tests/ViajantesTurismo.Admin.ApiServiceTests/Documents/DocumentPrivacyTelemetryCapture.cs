using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

/// <summary>Captures stopped activities from ASP.NET Core, HttpClient and a test parent source for privacy assertions.</summary>
internal sealed class DocumentPrivacyTelemetryCapture : IDisposable
{
    private readonly ConcurrentQueue<Activity> stoppedActivities = new();
    private readonly ActivitySource parentSource = new($"document-privacy-test-{Guid.CreateVersion7():N}");
    private readonly ActivityListener listener;

    /// <summary>Starts an activity listener that records stopped activities.</summary>
    [SuppressMessage("Testing", "SKTEST010", Justification = "Framework-owned sources expose names, not accessible singleton instances.")]
    public DocumentPrivacyTelemetryCapture()
    {
        listener = new ActivityListener
        {
            ShouldListenTo = source =>
                ReferenceEquals(source, parentSource)
                || source.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || source.Name.StartsWith("System.Net.Http", StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => stoppedActivities.Enqueue(activity),
        };
        ActivitySource.AddActivityListener(listener);
    }

    /// <summary>Starts the parent activity that correlates a test request with captured telemetry.</summary>
    public Activity StartParent() =>
        parentSource.StartActivity("document-privacy-request")
        ?? throw new InvalidOperationException("The document privacy parent activity was not created.");

    /// <summary>Returns the tags of every captured activity in the trace as key=value lines.</summary>
    public string GetCapturedText(ActivityTraceId traceId)
    {
        var values = stoppedActivities
            .Where(activity => activity.TraceId == traceId)
            .SelectMany(activity => activity.TagObjects.Select(tag => string.Concat(
                tag.Key,
                "=",
                Convert.ToString(tag.Value, CultureInfo.InvariantCulture))));
        return string.Join('\n', values);
    }

    /// <summary>Disposes the activity listener and parent source.</summary>
    public void Dispose()
    {
        listener.Dispose();
        parentSource.Dispose();
    }
}
