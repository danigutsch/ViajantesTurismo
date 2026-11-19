using System.Diagnostics;
using System.Diagnostics.Metrics;
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

    /// <summary>Captures migration duration measurements from a single host.</summary>
    public static MeterListener CreateDurationListener(
        IMeterFactory meterFactory,
        List<(string? Unit, double Duration, KeyValuePair<string, object?>[] Tags)> measurements)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, owner) =>
            {
                if (ReferenceEquals(instrument.Meter.Scope, meterFactory)
                    && instrument.Meter.Name == "ViajantesTurismo.MigrationService"
                    && instrument.Name == "migration.database_initialization.duration"
                    && instrument is Histogram<double>)
                {
                    owner.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => measurements.Add((instrument.Unit, value, tags.ToArray())));
        listener.Start();
        return listener;
    }

    /// <summary>Verifies the only dimension and seconds unit of the worker's terminal duration.</summary>
    public static void ShouldRecordInitializationDuration(
        this List<(string? Unit, double Duration, KeyValuePair<string, object?>[] Tags)> measurements,
        string outcome)
    {
        var (unit, duration, tags) = measurements.ShouldHaveSingleItem();
        unit.ShouldBe("s");
        duration.ShouldBeGreaterThanOrEqualTo(0);
        tags.ShouldBe([new KeyValuePair<string, object?>("migration.outcome", outcome)]);
    }
}
