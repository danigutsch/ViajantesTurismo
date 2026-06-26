using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using SharedKernel.EventSourcing;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public static class CatalogTelemetryTestsHelpers
{
    public static ActivityListener CreateActivityListener(ConcurrentQueue<Activity> stoppedActivities)
    {
        ArgumentNullException.ThrowIfNull(stoppedActivities);

        var listener = new ActivityListener
        {
            ShouldListenTo = static source => string.Equals(source.Name, CatalogTelemetry.Name, StringComparison.Ordinal),
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
        return Assert.Single(stoppedActivities, activity =>
            activity.TraceId == rootActivity.TraceId
            && string.Equals(activity.OperationName, operationName, StringComparison.Ordinal));
    }

    public static MeterListener CreateMeterListener(ConcurrentQueue<string> measurements)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = static (instrument, listener) =>
            {
                if (string.Equals(instrument.Meter.Name, CatalogTelemetry.Name, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };

        listener.SetMeasurementEventCallback<long>((instrument, _, _, _) => measurements.Enqueue(instrument.Name));
        listener.Start();
        return listener;
    }

    public static bool HasTag(Activity activity, string key, object expectedValue)
    {
        ArgumentNullException.ThrowIfNull(activity);

        foreach (var tag in activity.TagObjects)
        {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal) && Equals(tag.Value, expectedValue))
            {
                return true;
            }
        }

        return false;
    }

    public static EventEnvelope CreateEnvelope(long position, CatalogTourDraftCreated draftCreated, DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(draftCreated);

        return new EventEnvelope(
            CatalogTourStreamIds.FromAdminTourId(draftCreated.AdminTourId),
            position,
            StreamRevision.From(1),
            Guid.CreateVersion7(),
            nameof(CatalogTourDraftCreated),
            draftCreated,
            recordedAt);
    }
}
