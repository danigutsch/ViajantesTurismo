using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>Records the counter measurements of one host's meters, scoped by that host's meter factory.</summary>
internal sealed class MeterFactoryRecorder : IDisposable
{
    private readonly MeterListener listener = new();
    private readonly List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)> measurements = [];

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
        listener.Start();
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
