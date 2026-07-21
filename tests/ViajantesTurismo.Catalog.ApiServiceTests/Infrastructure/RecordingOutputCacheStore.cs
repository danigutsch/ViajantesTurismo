using Microsoft.AspNetCore.OutputCaching;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class RecordingOutputCacheStore : IOutputCacheStore
{
    public bool EvictionObserved { get; private set; }

    public CancellationToken EvictionCancellationToken { get; private set; }

    public Exception? EvictionException { get; set; }

    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<byte[]?>(null);
    }

    public ValueTask SetAsync(
        string key,
        byte[] value,
        string[]? tags,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        EvictionObserved = true;
        EvictionCancellationToken = cancellationToken;
        if (EvictionException is not null)
        {
            throw EvictionException;
        }

        return ValueTask.CompletedTask;
    }
}
