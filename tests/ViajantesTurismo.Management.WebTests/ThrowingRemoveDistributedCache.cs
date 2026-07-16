using Microsoft.Extensions.Caching.Distributed;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class ThrowingRemoveDistributedCache(
    IDistributedCache inner,
    Exception? failure = null,
    Func<string, int, bool>? shouldThrowOnRemove = null) : IDistributedCache
{
    private readonly Exception _failure = failure ?? new InvalidOperationException("The cache is unavailable.");
    private readonly Func<string, int, bool> _shouldThrowOnRemove = shouldThrowOnRemove ?? (static (_, _) => true);

    public int RemoveCalls { get; private set; }

    public CancellationToken LastRemoveCancellationToken { get; private set; }

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
        LastRemoveCancellationToken = CancellationToken.None;
        if (_shouldThrowOnRemove(key, RemoveCalls))
        {
            throw _failure;
        }

        inner.Remove(key);
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        RemoveCalls++;
        LastRemoveCancellationToken = token;
        return _shouldThrowOnRemove(key, RemoveCalls)
            ? Task.FromException(_failure)
            : inner.RemoveAsync(key, token);
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
