using Microsoft.Extensions.Caching.Distributed;
namespace ViajantesTurismo.Management.WebIntegrationTests;

/// <summary>Distributed cache decorator that suspends the first asynchronous write until released.</summary>
/// <param name="inner">Cache that receives all operations.</param>
internal sealed class BlockingFirstSetDistributedCache(IDistributedCache inner) : IDistributedCache
{
    private readonly TaskCompletionSource _firstSetStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseFirstSet = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _firstSetPending = 1;

    /// <summary>Forwards the synchronous read to the inner cache.</summary>
    public byte[]? Get(string key)
    {
        return inner.Get(key);
    }

    /// <summary>Forwards the asynchronous read to the inner cache.</summary>
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        return inner.GetAsync(key, token);
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

    /// <summary>Holds the first write until released, then writes to the inner cache.</summary>
    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        if (Interlocked.CompareExchange(ref _firstSetPending, 0, 1) == 1)
        {
            _firstSetStarted.TrySetResult();
            await _releaseFirstSet.Task.WaitAsync(token);
        }

        await inner.SetAsync(key, value, options, token);
    }

    /// <summary>Completes once the first write has started and is being held.</summary>
    public Task WaitForFirstSet(CancellationToken ct)
    {
        return _firstSetStarted.Task.WaitAsync(ct);
    }

    /// <summary>Releases the held first write.</summary>
    public void ReleaseFirstSet()
    {
        _releaseFirstSet.TrySetResult();
    }
}
