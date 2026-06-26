using System.Collections.Concurrent;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace ViajantesTurismo.Common.UnitTests.Observability;

internal sealed class CollectingMetricExporter(ConcurrentQueue<string> exportedMetricNames) : BaseExporter<Metric>
{
    public override ExportResult Export(in Batch<Metric> batch)
    {
        foreach (var metric in batch)
        {
            exportedMetricNames.Enqueue(metric.Name);
        }

        return ExportResult.Success;
    }
}
