using System.Diagnostics;
using System.Reflection;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal static class SeederWorkerTestHelpers
{
    public static async Task ExecuteWorker(SeederWorker worker, CancellationToken ct)
    {
        var executeAsync = typeof(SeederWorker).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        _ = (executeAsync).ShouldNotBeNull();
        var executionTask = (Task?)executeAsync.Invoke(worker, [ct]);
        _ = (executionTask).ShouldNotBeNull();
        await executionTask;
    }

    public static ActivityListener CreateCapturingListener(List<Activity> stoppedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == SeederWorker.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Add,
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
