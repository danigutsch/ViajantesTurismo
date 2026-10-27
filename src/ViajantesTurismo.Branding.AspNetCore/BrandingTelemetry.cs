using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>
/// Emits Branding public-cache metrics. Instruments carry an outcome or the source revision only, never Branding values.
/// </summary>
internal sealed class BrandingTelemetry : IDisposable
{
    public const string MeterName = "ViajantesTurismo.Branding";

    public const string CacheRequests = "branding.cache.requests";

    public const string CacheInvalidations = "branding.cache.invalidations";

    public const string SourceRevision = "branding.settings.source_revision";

    private readonly Meter meter;

    private readonly Counter<long> cacheRequests;

    private readonly Counter<long> cacheInvalidations;

    private long sourceRevision;

    public BrandingTelemetry(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        meter = meterFactory.Create(MeterName);
        cacheRequests = meter.CreateCounter<long>(CacheRequests, description: "Public Branding requests by cache outcome.");
        cacheInvalidations = meter.CreateCounter<long>(CacheInvalidations, description: "Public Branding cache invalidations by outcome.");
        _ = meter.CreateObservableGauge(
            SourceRevision,
            () => Volatile.Read(ref sourceRevision),
            description: "Latest Branding source revision this instance committed or read.");
    }

    public void Dispose() => meter.Dispose();

    public void CacheHit() => cacheRequests.Add(1, new KeyValuePair<string, object?>("outcome", "hit"));

    public void CacheMiss() => cacheRequests.Add(1, new KeyValuePair<string, object?>("outcome", "miss"));

    public void Invalidation(bool succeeded) =>
        cacheInvalidations.Add(1, new KeyValuePair<string, object?>("outcome", succeeded ? "succeeded" : "failed"));

    public void ObserveRevision(long revision)
    {
        long current;
        do
        {
            current = Volatile.Read(ref sourceRevision);
        }
        while (revision > current && Interlocked.CompareExchange(ref sourceRevision, revision, current) != current);
    }
}
