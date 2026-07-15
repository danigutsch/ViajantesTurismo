using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using SharedKernel.Observability.Npgsql;

namespace ViajantesTurismo.Admin.IntegrationTests.Observability;

internal static class PostgreSqlIndexHealthTelemetryTestListener
{
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
