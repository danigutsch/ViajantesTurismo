using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Records the measurements of one host's Branding meter, scoped by that host's meter factory.</summary>
internal sealed class BrandingMetricRecorder : IDisposable
{
    private readonly MeterListener listener = new();
    private readonly List<(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)> measurements = [];

    public BrandingMetricRecorder(IMeterFactory meterFactory)
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

    public void Observe() => listener.RecordObservableInstruments();

    public long Sum(string instrument, string tagName, string tagValue)
    {
        lock (measurements)
        {
            return measurements
                .Where(m => m.Instrument == instrument && m.Tags.Any(t => t.Key == tagName && Equals(t.Value, tagValue)))
                .Sum(m => m.Value);
        }
    }

    public long Last(string instrument)
    {
        lock (measurements)
        {
            return measurements.Last(m => m.Instrument == instrument).Value;
        }
    }

    public string[] TagNames(string instrument)
    {
        lock (measurements)
        {
            return measurements
                .Where(m => m.Instrument == instrument)
                .SelectMany(m => m.Tags.Select(t => t.Key))
                .Distinct()
                .Order()
                .ToArray();
        }
    }

    public void Dispose() => listener.Dispose();
}
