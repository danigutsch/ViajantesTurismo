using Microsoft.AspNetCore.OutputCaching;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>An output cache store that stores nothing and records tag eviction calls.</summary>
internal sealed class RecordingOutputCacheStore : IOutputCacheStore
{
    /// <summary>Whether an eviction was requested.</summary>
    public bool EvictionObserved
    {
        get; private set;
    }

    /// <summary>Tag of the last eviction.</summary>
    public string? EvictionTag
    {
        get; private set;
    }

    /// <summary>Cancellation token of the last eviction.</summary>
    public CancellationToken EvictionCancellationToken
    {
        get; private set;
    }

    /// <summary>Exception thrown on eviction, when set.</summary>
    public Exception? EvictionException
    {
        get; set;
    }

    /// <summary>Always returns a cache miss.</summary>
    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<byte[]?>(null);
    }

    /// <summary>Discards the entry.</summary>
    public ValueTask SetAsync(
        string key,
        byte[] value,
        string[]? tags,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>Records the eviction and throws <see cref="EvictionException" /> when set.</summary>
    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        EvictionObserved = true;
        EvictionTag = tag;
        EvictionCancellationToken = cancellationToken;
        if (EvictionException is not null)
        {
            throw EvictionException;
        }

        return ValueTask.CompletedTask;
    }
}
