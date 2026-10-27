using System.Diagnostics;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

/// <summary>
/// Helpers for running and observing the database initialization worker in tests.
/// </summary>
internal static class DatabaseInitializationWorkerTestHelpers
{
    /// <summary>
    /// Runs <paramref name="worker"/> once to completion.
    /// </summary>
    public static async Task ExecuteWorker(DatabaseInitializationWorker worker, CancellationToken ct)
    {
        await worker.Run(ct);
    }

    /// <summary>
    /// Creates and registers a listener that records stopped activities from <paramref name="activitySource"/> into <paramref name="stoppedActivities"/>.
    /// </summary>
    public static ActivityListener CreateCapturingListener(
        ActivitySource activitySource,
        List<Activity> stoppedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, activitySource),
            Sample = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Add,
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
