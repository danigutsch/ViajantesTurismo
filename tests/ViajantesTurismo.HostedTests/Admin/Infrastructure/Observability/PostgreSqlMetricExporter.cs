using System.Collections.Concurrent;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Observability;

/// <summary>Captures native PostgreSQL metric dimensions delivered to an OpenTelemetry exporter.</summary>
internal sealed class PostgreSqlMetricExporter : BaseExporter<Metric>
{
    private readonly ConcurrentQueue<(string Instrument, IReadOnlyList<KeyValuePair<string, object?>> Tags)> points = new();

    /// <summary>Gets a snapshot of all exported native metric points.</summary>
    public IReadOnlyList<(string Instrument, IReadOnlyList<KeyValuePair<string, object?>> Tags)> Points => [.. points];

    /// <inheritdoc />
    public override ExportResult Export(in Batch<Metric> batch)
    {
        foreach (var metric in batch)
        {
            if (metric.MeterName != "Npgsql")
            {
                continue;
            }

            foreach (ref readonly var point in metric.GetMetricPoints())
            {
                var tags = new List<KeyValuePair<string, object?>>(point.Tags.Count);
                foreach (var tag in point.Tags)
                {
                    tags.Add(tag);
                }

                points.Enqueue((metric.Name, tags));
            }
        }

        return ExportResult.Success;
    }
}
