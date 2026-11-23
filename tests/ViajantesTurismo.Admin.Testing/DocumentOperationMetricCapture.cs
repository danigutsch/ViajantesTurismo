using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Admin.Testing;

/// <summary>Captures operation counts emitted by the document meter of one test host.</summary>
public sealed class DocumentOperationMetricCapture : IDisposable
{
    private readonly MeterListener listener = new();

    /// <summary>Subscribes to the given host's document-operation counter.</summary>
    public DocumentOperationMetricCapture(IMeterFactory meterFactory)
    {
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (ReferenceEquals(instrument.Meter.Scope, meterFactory)
                && instrument.Name == "admin.document.operation")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, count, tags, _) => Measurements.Enqueue((count, tags.ToArray())));
        listener.Start();
    }

    /// <summary>Gets the value and tags of each recorded counter increment.</summary>
    public ConcurrentQueue<(long Count, KeyValuePair<string, object?>[] Tags)> Measurements { get; } = new();

    /// <summary>Stops listening to the host's meter.</summary>
    public void Dispose() => listener.Dispose();
}
