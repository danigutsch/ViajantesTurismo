using System.Diagnostics.Metrics;
using Npgsql;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Reads the tags the native Npgsql pool metrics emit for the data sources alive in the process.</summary>
internal static class NpgsqlPoolMetricTagCapture
{
    private const string NpgsqlMeterName = "Npgsql";
    private const string PoolNameTagKey = "db.client.connection.pool.name";

    /// <summary>Builds a data source from the connection string and returns the tags emitted for its pool.</summary>
    /// <param name="connectionString">The connection string, which may contain a password.</param>
    /// <param name="dataSourceName">The data source name, which is also the expected pool name tag value.</param>
    public static IReadOnlyList<KeyValuePair<string, string>> CaptureForNamedDataSource(string connectionString, string dataSourceName)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString)
        {
            Name = dataSourceName
        };

        using var dataSource = builder.Build();

        return [.. ObserveMeasurements()
            .Where(tags => tags.Contains(new KeyValuePair<string, string>(PoolNameTagKey, dataSourceName)))
            .SelectMany(static tags => tags)];
    }

    /// <summary>Returns the distinct pool names reported by the data sources alive in the process.</summary>
    public static IReadOnlySet<string> ObservePoolNames()
    {
        return ObserveMeasurements()
            .SelectMany(static tags => tags)
            .Where(static tag => tag.Key == PoolNameTagKey)
            .Select(static tag => tag.Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static List<List<KeyValuePair<string, string>>> ObserveMeasurements()
    {
        var measurements = new List<List<KeyValuePair<string, string>>>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = static (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == NpgsqlMeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => measurements.Add(ToPairs(tags)));
        listener.SetMeasurementEventCallback<int>((_, _, tags, _) => measurements.Add(ToPairs(tags)));
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) => measurements.Add(ToPairs(tags)));
        listener.Start();
        listener.RecordObservableInstruments();

        return measurements;
    }

    private static List<KeyValuePair<string, string>> ToPairs(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var pairs = new List<KeyValuePair<string, string>>(tags.Length);
        foreach (var tag in tags)
        {
            pairs.Add(new KeyValuePair<string, string>(tag.Key, tag.Value?.ToString() ?? string.Empty));
        }

        return pairs;
    }
}
