using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;

namespace ViajantesTurismo.Common.UnitTests.Observability;

internal sealed class CollectingActivityExporter(ConcurrentQueue<Activity> exportedActivities) : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            exportedActivities.Enqueue(activity);
        }

        return ExportResult.Success;
    }
}
