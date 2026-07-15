using Microsoft.Extensions.Caching.Distributed;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class ThrowingRemoveDistributedCache(IDistributedCache inner, Exception? failure = null) : IDistributedCache
{
    private readonly Exception _failure = failure ?? new InvalidOperationException("The cache is unavailable.");

    public int RemoveCalls { get; private set; }

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
        RemoveCalls++;
        throw _failure;
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        RemoveCalls++;
        return Task.FromException(_failure);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        inner.Set(key, value, options);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        return inner.SetAsync(key, value, options, token);
    }
}
