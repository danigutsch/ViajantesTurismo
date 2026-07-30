namespace ViajantesTurismo.Management.WebIntegrationTests;

internal sealed class TrackingAsyncDisposable(
    string name,
    ICollection<string> disposalOrder,
    Exception? failure = null) : IAsyncDisposable
{
    private int _disposed;

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        disposalOrder.Add(name);
        return failure is null
            ? ValueTask.CompletedTask
            : ValueTask.FromException(failure);
    }
}
