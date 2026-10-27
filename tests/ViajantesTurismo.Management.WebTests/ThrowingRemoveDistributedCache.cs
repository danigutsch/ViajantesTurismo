using Microsoft.Extensions.Caching.Distributed;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Distributed cache decorator that can throw on remove and records remove calls.</summary>
/// <param name="inner">Cache that receives all calls that do not throw.</param>
/// <param name="failure">Exception thrown on failing removes; defaults to <see cref="InvalidOperationException"/>.</param>
/// <param name="shouldThrowOnRemove">Receives key and 1-based remove call count; returns true to throw. Always true by default.</param>
internal sealed class ThrowingRemoveDistributedCache(
    IDistributedCache inner,
    Exception? failure = null,
    Func<string, int, bool>? shouldThrowOnRemove = null) : IDistributedCache
{
    private readonly Exception _failure = failure ?? new InvalidOperationException("The cache is unavailable.");
    private readonly Func<string, int, bool> _shouldThrowOnRemove = shouldThrowOnRemove ?? (static (_, _) => true);

    /// <summary>Number of sync and async remove calls received.</summary>
    public int RemoveCalls
    {
        get; private set;
    }

    /// <summary>Cancellation token passed to the latest remove; <see cref="CancellationToken.None"/> for sync removes.</summary>
    public CancellationToken LastRemoveCancellationToken
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

    /// <summary>Counts the call, then throws the configured failure or removes from the inner cache.</summary>
    public void Remove(string key)
    {
        RemoveCalls++;
        LastRemoveCancellationToken = CancellationToken.None;
        if (_shouldThrowOnRemove(key, RemoveCalls))
        {
            throw _failure;
        }

        inner.Remove(key);
    }

    /// <summary>Counts the call and records the token, then returns a faulted task or removes from the inner cache.</summary>
    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        RemoveCalls++;
        LastRemoveCancellationToken = token;
        return _shouldThrowOnRemove(key, RemoveCalls)
            ? Task.FromException(_failure)
            : inner.RemoveAsync(key, token);
    }

    /// <summary>Forwards to the inner cache.</summary>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        inner.Set(key, value, options);
    }

    /// <summary>Forwards to the inner cache.</summary>
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        return inner.SetAsync(key, value, options, token);
    }
}
