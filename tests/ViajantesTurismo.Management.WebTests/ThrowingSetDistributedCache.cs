using Microsoft.Extensions.Caching.Distributed;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Distributed cache decorator that can throw on set and records the latest entry options.</summary>
/// <param name="inner">Cache that receives all calls that do not throw.</param>
/// <param name="shouldThrowOnSet">Receives key and 1-based set call count; returns true to throw.</param>
/// <param name="failure">Exception thrown on failing sets; defaults to <see cref="InvalidOperationException"/>.</param>
internal sealed class ThrowingSetDistributedCache(
    IDistributedCache inner,
    Func<string, int, bool> shouldThrowOnSet,
    Exception? failure = null) : IDistributedCache
{
    private readonly Exception _failure = failure ?? new InvalidOperationException("The cache is unavailable.");
    private int _setCalls;

    /// <summary>Entry options passed to the latest set, whether or not it threw.</summary>
    public DistributedCacheEntryOptions? LastOptions
    {
        get; private set;
    }

    /// <summary>Forwards to the inner cache.</summary>
    public byte[]? Get(string key)
    {
        return inner.Get(key);
    }

    /// <summary>Forwards to the inner cache.</summary>
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        return inner.GetAsync(key, token);
    }

    /// <summary>Forwards to the inner cache.</summary>
    public void Refresh(string key)
    {
        inner.Refresh(key);
    }

    /// <summary>Forwards to the inner cache.</summary>
    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        return inner.RefreshAsync(key, token);
    }

    /// <summary>Forwards to the inner cache.</summary>
    public void Remove(string key)
    {
        inner.Remove(key);
    }

    /// <summary>Forwards to the inner cache.</summary>
    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        return inner.RemoveAsync(key, token);
    }

    /// <summary>Records options and counts the call, then throws the configured failure or sets in the inner cache.</summary>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        LastOptions = options;
        var setCall = Interlocked.Increment(ref _setCalls);
        if (shouldThrowOnSet(key, setCall))
        {
            throw _failure;
        }

        inner.Set(key, value, options);
    }

    /// <summary>Records options and counts the call, then returns a faulted task or sets in the inner cache.</summary>
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        LastOptions = options;
        var setCall = Interlocked.Increment(ref _setCalls);
        return shouldThrowOnSet(key, setCall)
            ? Task.FromException(_failure)
            : inner.SetAsync(key, value, options, token);
    }
}
