using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using PandaBear.Observability.Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Observability;

/// <summary>Test listener that captures index-health meter measurements as tag strings.</summary>
internal static class PostgreSqlIndexHealthTelemetryTestListener
{
    /// <summary>Starts a listener on the index-health meter that enqueues each tag as <c>instrument:key=value</c> into <paramref name="recordedTags" />.</summary>
    public static MeterListener Create(ConcurrentQueue<string> recordedTags)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = static (instrument, meterListener) =>
            {
                if (string.Equals(instrument.Meter.Name, PostgreSqlIndexHealthTelemetry.MeterName, StringComparison.Ordinal))
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };

        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            foreach (var tag in tags)
            {
                recordedTags.Enqueue($"{instrument.Name}:{tag.Key}={tag.Value}");
            }
        });
        listener.Start();
        return listener;
    }
}
