using Microsoft.Extensions.Caching.Distributed;
namespace ViajantesTurismo.Management.WebIntegrationTests;

/// <summary>Distributed cache decorator that suspends the first asynchronous read of one key until released.</summary>
/// <param name="inner">Cache that receives all operations.</param>
/// <param name="blockedKey">Key whose first <c>GetAsync</c> call is held.</param>
internal sealed class BlockingFirstGetDistributedCache(IDistributedCache inner, string blockedKey) : IDistributedCache
{
    private readonly TaskCompletionSource _firstGetStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseFirstGet = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _firstGetPending = 1;

    /// <summary>Forwards the synchronous read to the inner cache.</summary>
    public byte[]? Get(string key)
    {
        return inner.Get(key);
    }

    /// <summary>Reads from the inner cache, then holds the first read of the blocked key until released.</summary>
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

    /// <summary>Forwards the synchronous refresh to the inner cache.</summary>
    public void Refresh(string key)
    {
        inner.Refresh(key);
    }

    /// <summary>Forwards the asynchronous refresh to the inner cache.</summary>
    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        return inner.RefreshAsync(key, token);
    }

    /// <summary>Forwards the synchronous removal to the inner cache.</summary>
    public void Remove(string key)
    {
        inner.Remove(key);
    }

    /// <summary>Forwards the asynchronous removal to the inner cache.</summary>
    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        return inner.RemoveAsync(key, token);
    }

    /// <summary>Forwards the synchronous write to the inner cache.</summary>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        inner.Set(key, value, options);
    }

    /// <summary>Forwards the asynchronous write to the inner cache.</summary>
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        return inner.SetAsync(key, value, options, token);
    }

    /// <summary>Completes once the first read of the blocked key has started and is being held.</summary>
    public Task WaitForFirstGet(CancellationToken ct)
    {
        return _firstGetStarted.Task.WaitAsync(ct);
    }

    /// <summary>Awaits the operation, then releases the held read even if the operation fails.</summary>
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
