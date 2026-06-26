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

        Assert.NotNull(executeAsync);
        var executionTask = (Task?)executeAsync.Invoke(worker, [ct]);
        Assert.NotNull(executionTask);
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
