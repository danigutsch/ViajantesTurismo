namespace ViajantesTurismo.Management.WebIntegrationTests;

internal sealed class BlockingFirstGetDistributedCache(IDistributedCache inner, string blockedKey) : IDistributedCache
{
    private readonly TaskCompletionSource _firstGetStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseFirstGet = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _firstGetPending = 1;

    public byte[]? Get(string key)
    {
        return inner.Get(key);
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var value = await inner.GetAsync(key, token);
        if (key == blockedKey && Interlocked.CompareExchange(ref _firstGetPending, 0, 1) == 1)
        {
            _firstGetStarted.TrySetResult();
            await _releaseFirstGet.Task.WaitAsync(token);
        }

        return value;
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

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        return inner.SetAsync(key, value, options, token);
    }

    public Task WaitForFirstGet(CancellationToken ct)
    {
        return _firstGetStarted.Task.WaitAsync(ct);
    }

    public async Task CompleteThenRelease(Task operation)
    {
        try
        {
            await operation;
        }
        finally
        {
            _releaseFirstGet.TrySetResult();
        }
    }
}
