using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Records measurements from one host's meter factory.</summary>
internal sealed class MeterFactoryRecorder : IDisposable
{
    private readonly MeterListener listener = new();
    private readonly List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)> measurements = [];
    private readonly List<(string Instrument, double Value, KeyValuePair<string, object?>[] Tags)> durations = [];

    /// <summary>Starts listening to instruments whose meter is scoped by the given meter factory.</summary>
    public MeterFactoryRecorder(IMeterFactory meterFactory)
    {
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (ReferenceEquals(instrument.Meter.Scope, meterFactory))
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            lock (measurements)
            {
                measurements.Add((instrument.Name, value, tags.ToArray()));
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            lock (durations)
            {
                durations.Add((instrument.Name, value, tags.ToArray()));
            }
        });
        listener.Start();
    }

    /// <summary>Returns counter measurements for the named instrument.</summary>
    public (long Value, KeyValuePair<string, object?>[] Tags)[] Counters(string instrument)
    {
        lock (measurements)
        {
            return [.. measurements.Where(m => m.Instrument == instrument).Select(m => (m.Value, m.Tags))];
        }
    }

    /// <summary>Returns histogram measurements for the named instrument.</summary>
    public (double Value, KeyValuePair<string, object?>[] Tags)[] Histograms(string instrument)
    {
        lock (durations)
        {
            return [.. durations.Where(m => m.Instrument == instrument).Select(m => (m.Value, m.Tags))];
        }
    }

    /// <summary>Sums the recorded values of the instrument whose tag matches the given name and value.</summary>
    public long Sum(string instrument, string tagName, string tagValue)
    {
        lock (measurements)
        {
            return measurements
                .Where(m => m.Instrument == instrument && m.Tags.Any(t => t.Key == tagName && Equals(t.Value, tagValue)))
                .Sum(m => m.Value);
        }
    }

    /// <summary>Stops listening to instruments.</summary>
    public void Dispose() => listener.Dispose();
}
