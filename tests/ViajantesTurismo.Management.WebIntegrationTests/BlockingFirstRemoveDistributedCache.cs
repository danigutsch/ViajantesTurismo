namespace ViajantesTurismo.Management.WebIntegrationTests;

internal sealed class BlockingFirstRemoveDistributedCache(IDistributedCache inner) : IDistributedCache
{
    private readonly TaskCompletionSource _firstRemoveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseFirstRemove = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _firstRemovePending = 1;

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

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        if (Interlocked.CompareExchange(ref _firstRemovePending, 0, 1) == 1)
        {
            _firstRemoveStarted.TrySetResult();
            await _releaseFirstRemove.Task.WaitAsync(token);
        }

        await inner.RemoveAsync(key, token);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        inner.Set(key, value, options);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        return inner.SetAsync(key, value, options, token);
    }

    public Task WaitForFirstRemove(CancellationToken ct)
    {
        return _firstRemoveStarted.Task.WaitAsync(ct);
    }

    public void ReleaseFirstRemove()
    {
        _releaseFirstRemove.TrySetResult();
    }
}
