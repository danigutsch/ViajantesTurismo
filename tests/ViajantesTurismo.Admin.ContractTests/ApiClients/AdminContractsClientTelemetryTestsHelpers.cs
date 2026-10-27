using System.Collections.Concurrent;
using System.Diagnostics;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

/// <summary>
/// Creates activity listeners and activities for the contracts client telemetry tests.
/// </summary>
internal static class AdminContractsClientTelemetryTestsHelpers
{
    /// <summary>
    /// Creates a listener that records every stopped activity of the contracts client source into the given queue.
    /// </summary>
    public static ActivityListener CreateActivityListener(ConcurrentQueue<Activity> stoppedActivities)
    {
        ArgumentNullException.ThrowIfNull(stoppedActivities);

        var expectedSource = AdminContractsClientTelemetry.ActivitySource;
        var listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, expectedSource),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    /// <summary>
    /// Starts a root activity that client activities in a test are expected to share a trace with.
    /// </summary>
    public static Activity StartRootActivity()
    {
        var activity = new Activity("test.root");
        activity.Start();
        return activity;
    }

    /// <summary>
    /// Returns the only stopped activity in the root trace with the given operation name, failing otherwise.
    /// </summary>
    public static Activity SingleActivity(
        ConcurrentQueue<Activity> stoppedActivities,
        Activity rootActivity,
        string operationName)
    {
        return stoppedActivities.ShouldHaveSingleItem(activity =>
            activity.TraceId == rootActivity.TraceId
            && string.Equals(activity.OperationName, operationName, StringComparison.Ordinal));
    }
}
