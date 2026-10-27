using System.Collections.Concurrent;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace ViajantesTurismo.ServiceDefaults.Tests;

/// <summary>Test exporter that collects exported metric names.</summary>
/// <param name="exportedMetricNames">Queue receiving each exported metric name.</param>
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
