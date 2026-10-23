using System.Collections.Concurrent;
using System.Diagnostics;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

internal static class AdminContractsClientTelemetryTestsHelpers
{
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

    public static Activity StartRootActivity()
    {
        var activity = new Activity("test.root");
        activity.Start();
        return activity;
    }

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
