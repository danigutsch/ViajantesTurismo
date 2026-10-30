using Microsoft.Extensions.Caching.Distributed;
namespace ViajantesTurismo.HostedTests.Management.Security;

/// <summary>Distributed cache decorator that suspends the first asynchronous removal until released.</summary>
/// <param name="inner">Cache that receives all operations.</param>
internal sealed class BlockingFirstRemoveDistributedCache(IDistributedCache inner) : IDistributedCache
{
    private readonly TaskCompletionSource _firstRemoveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseFirstRemove = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _firstRemovePending = 1;

    /// <summary>Forwards the synchronous read to the inner cache.</summary>
    public byte[]? Get(string key)
    {
        return inner.Get(key);
    }

    /// <summary>Forwards the asynchronous read to the inner cache.</summary>
    public Task<byte[]?> GetAsync(string key, CancellationToken ct)
    {
        return inner.GetAsync(key, ct);
    }

    /// <summary>Forwards the synchronous refresh to the inner cache.</summary>
    public void Refresh(string key)
    {
        inner.Refresh(key);
    }

    /// <summary>Forwards the asynchronous refresh to the inner cache.</summary>
    public Task RefreshAsync(string key, CancellationToken ct)
    {
        return inner.RefreshAsync(key, ct);
    }

    /// <summary>Forwards the synchronous removal to the inner cache.</summary>
    public void Remove(string key)
    {
        inner.Remove(key);
    }

    /// <summary>Holds the first removal until released, then removes the key from the inner cache.</summary>
    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _firstRemovePending, 0, 1) == 1)
        {
            _firstRemoveStarted.TrySetResult();
            await _releaseFirstRemove.Task.WaitAsync(ct);
        }

        await inner.RemoveAsync(key, ct);
    }

    /// <summary>Forwards the synchronous write to the inner cache.</summary>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        inner.Set(key, value, options);
    }

    /// <summary>Forwards the asynchronous write to the inner cache.</summary>
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken ct)
    {
        return inner.SetAsync(key, value, options, ct);
    }

    /// <summary>Completes once the first removal has started and is being held.</summary>
    public Task WaitForFirstRemove(CancellationToken ct)
    {
        return _firstRemoveStarted.Task.WaitAsync(ct);
    }

    /// <summary>Releases the held first removal.</summary>
    public void ReleaseFirstRemove()
    {
        _releaseFirstRemove.TrySetResult();
    }
}
