using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>
/// Emits Branding public-cache metrics. Instruments carry an outcome or the source revision only, never Branding values.
/// </summary>
internal sealed class BrandingTelemetry : IDisposable
{
    /// <summary>Name of the Branding meter.</summary>
    public const string MeterName = "ViajantesTurismo.Branding";

    /// <summary>Instrument name counting public requests by cache outcome.</summary>
    public const string CacheRequests = "branding.cache.requests";

    /// <summary>Instrument name counting cache invalidations by outcome.</summary>
    public const string CacheInvalidations = "branding.cache.invalidations";

    /// <summary>Instrument name of the latest observed source revision gauge.</summary>
    public const string SourceRevision = "branding.settings.source_revision";

    private readonly Meter meter;

    private readonly Counter<long> cacheRequests;

    private readonly Counter<long> cacheInvalidations;

    private long sourceRevision;

    /// <summary>Creates the meter and its instruments from the given factory.</summary>
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

    /// <inheritdoc />
    public void Dispose() => meter.Dispose();

    /// <summary>Counts a public request served from the cache.</summary>
    public void CacheHit() => cacheRequests.Add(1, new KeyValuePair<string, object?>("outcome", "hit"));

    /// <summary>Counts a public request that missed the cache.</summary>
    public void CacheMiss() => cacheRequests.Add(1, new KeyValuePair<string, object?>("outcome", "miss"));

    /// <summary>Counts a cache invalidation as succeeded or failed.</summary>
    public void Invalidation(bool succeeded) =>
        cacheInvalidations.Add(1, new KeyValuePair<string, object?>("outcome", succeeded ? "succeeded" : "failed"));

    /// <summary>Records a revision, keeping only the highest value seen.</summary>
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
