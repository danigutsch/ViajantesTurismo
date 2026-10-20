using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

internal sealed class DocumentPrivacyTelemetryCapture : IDisposable
{
    private readonly ConcurrentQueue<Activity> stoppedActivities = new();
    private readonly ActivitySource parentSource = new($"document-privacy-test-{Guid.CreateVersion7():N}");
    private readonly ActivityListener listener;

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

    public Activity StartParent() =>
        parentSource.StartActivity("document-privacy-request")
        ?? throw new InvalidOperationException("The document privacy parent activity was not created.");

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

    public void Dispose()
    {
        listener.Dispose();
        parentSource.Dispose();
    }
}
