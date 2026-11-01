using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Reads the pool names the native Npgsql pool metrics report for the data sources alive in the process.</summary>
internal static class NpgsqlPoolNameObserver
{
    private const string NpgsqlMeterName = "Npgsql";
    private const string PoolNameTagKey = "db.client.connection.pool.name";

    /// <summary>Returns the distinct pool names reported right now.</summary>
    public static IReadOnlySet<string> Observe()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        using var listener = new MeterListener();
        listener.InstrumentPublished = static (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == NpgsqlMeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => Collect(tags, names));
        listener.SetMeasurementEventCallback<int>((_, _, tags, _) => Collect(tags, names));
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) => Collect(tags, names));
        listener.Start();
        listener.RecordObservableInstruments();

        return names;
    }

    private static void Collect(ReadOnlySpan<KeyValuePair<string, object?>> tags, HashSet<string> names)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == PoolNameTagKey && tag.Value is string name)
            {
                names.Add(name);
            }
        }
    }
}
