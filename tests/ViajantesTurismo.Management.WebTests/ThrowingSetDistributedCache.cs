using Microsoft.Extensions.Caching.Distributed;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class ThrowingSetDistributedCache(
    IDistributedCache inner,
    Func<string, int, bool> shouldThrowOnSet,
    Exception? failure = null) : IDistributedCache
{
    private readonly Exception _failure = failure ?? new InvalidOperationException("The cache is unavailable.");
    private int _setCalls;

    public byte[]? Get(string key)
    {
        return inner.Get(key);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        return inner.GetAsync(key, token);
    }

    public void Refresh(string key)
    {
        inner.Refresh(key);
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        return inner.RefreshAsync(key, token);
    }

    public void Remove(string key)
    {
        inner.Remove(key);
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        return inner.RemoveAsync(key, token);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var setCall = Interlocked.Increment(ref _setCalls);
        if (shouldThrowOnSet(key, setCall))
        {
            throw _failure;
        }

        inner.Set(key, value, options);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var setCall = Interlocked.Increment(ref _setCalls);
        return shouldThrowOnSet(key, setCall)
            ? Task.FromException(_failure)
            : inner.SetAsync(key, value, options, token);
    }
}
