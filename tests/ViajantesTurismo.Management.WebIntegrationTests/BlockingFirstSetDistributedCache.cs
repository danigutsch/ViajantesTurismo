namespace ViajantesTurismo.Management.WebIntegrationTests;

internal sealed class BlockingFirstSetDistributedCache(IDistributedCache inner) : IDistributedCache
{
    private readonly TaskCompletionSource _firstSetStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseFirstSet = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _firstSetPending = 1;

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
        inner.Set(key, value, options);
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        if (Interlocked.CompareExchange(ref _firstSetPending, 0, 1) == 1)
        {
            _firstSetStarted.TrySetResult();
            await _releaseFirstSet.Task.WaitAsync(token);
        }

        await inner.SetAsync(key, value, options, token);
    }

    public Task WaitForFirstSet(CancellationToken ct)
    {
        return _firstSetStarted.Task.WaitAsync(ct);
    }

    public void ReleaseFirstSet()
    {
        _releaseFirstSet.TrySetResult();
    }
}
