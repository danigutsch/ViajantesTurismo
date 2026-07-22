using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;

namespace ViajantesTurismo.ServiceDefaults.Tests;

internal sealed class CollectingActivityExporter(
    ConcurrentQueue<Activity> exportedActivities,
    Action<Activity>? activityExported = null) : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            exportedActivities.Enqueue(activity);
            activityExported?.Invoke(activity);
        }

        return ExportResult.Success;
    }
}
