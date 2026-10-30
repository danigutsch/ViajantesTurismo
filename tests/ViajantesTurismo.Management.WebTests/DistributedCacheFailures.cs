using Microsoft.Extensions.Caching.Distributed;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Builds shared <see cref="ThrowingDistributedCache"/> instances that fail only one operation kind.</summary>
internal static class DistributedCacheFailures
{
    /// <summary>Creates a cache that throws on remove when <paramref name="when"/> returns true and forwards everything else.</summary>
    /// <param name="inner">Cache that receives calls that do not throw.</param>
    /// <param name="failure">Exception thrown; <see cref="InvalidOperationException"/> when null.</param>
    /// <param name="when">Receives key and 1-based remove call count; always true when null.</param>
    public static ThrowingDistributedCache Remove(
        IDistributedCache inner,
        Exception? failure = null,
        Func<string, int, bool>? when = null)
    {
        return Create(inner, DistributedCacheOperation.Remove, failure, when ?? (static (_, _) => true));
    }

    /// <summary>Creates a cache that throws on set when <paramref name="when"/> returns true and forwards everything else.</summary>
    /// <param name="inner">Cache that receives calls that do not throw.</param>
    /// <param name="when">Receives key and 1-based set call count.</param>
    /// <param name="failure">Exception thrown; <see cref="InvalidOperationException"/> when null.</param>
    public static ThrowingDistributedCache Set(
        IDistributedCache inner,
        Func<string, int, bool> when,
        Exception? failure = null)
    {
        return Create(inner, DistributedCacheOperation.Set, failure, when);
    }

    private static ThrowingDistributedCache Create(
        IDistributedCache inner,
        DistributedCacheOperation operation,
        Exception? failure,
        Func<string, int, bool> when)
    {
        return new ThrowingDistributedCache(
            inner,
            failure ?? new InvalidOperationException("The cache is unavailable."),
            (op, key, call) => op == operation && when(key, call));
    }
}
