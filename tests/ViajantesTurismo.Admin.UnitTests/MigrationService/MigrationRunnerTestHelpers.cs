using System.Diagnostics;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal static class MigrationRunnerTestHelpers
{
    public static ActivityListener CreateCapturingListener(
        ActivitySource activitySource,
        List<Activity> stoppedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, activitySource),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Add,
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
