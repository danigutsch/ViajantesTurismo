using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;

namespace ViajantesTurismo.ServiceDefaults.Tests;

/// <summary>Test exporter that collects exported activities.</summary>
/// <param name="exportedActivities">Queue receiving each exported activity.</param>
/// <param name="activityExported">Optional callback invoked per exported activity.</param>
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
