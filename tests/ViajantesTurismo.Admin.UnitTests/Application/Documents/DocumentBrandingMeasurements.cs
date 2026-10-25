using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

internal sealed class DocumentBrandingMeasurements : IDisposable
{
    private readonly MeterListener _listener = new();

    public DocumentBrandingMeasurements()
    {
        _listener.InstrumentPublished = static (instrument, listener) =>
        {
            if (string.Equals(instrument.Meter.Name, DocumentBrandingMetrics.MeterName, StringComparison.Ordinal)
                && string.Equals(instrument.Name, DocumentBrandingMetrics.ProjectionMismatchName, StringComparison.Ordinal))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (string.Equals(tag.Key, DocumentBrandingMetrics.ReasonTagName, StringComparison.Ordinal))
                {
                    MismatchReasons.Enqueue(tag.Value?.ToString() ?? string.Empty);
                }
            }
        });
        _listener.Start();
    }

    public ConcurrentQueue<string> MismatchReasons { get; } = new();

    public void Dispose() => _listener.Dispose();
}
