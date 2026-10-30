namespace ViajantesTurismo.HostedTests.Management.Security;

/// <summary>Records its name in the disposal order on first disposal and optionally fails.</summary>
/// <param name="name">Name added to the disposal order.</param>
/// <param name="disposalOrder">Collection receiving names in disposal order.</param>
/// <param name="failure">Exception returned by disposal, or null to succeed.</param>
internal sealed class TrackingAsyncDisposable(
    string name,
    ICollection<string> disposalOrder,
    Exception? failure = null) : IAsyncDisposable
{
    private int _disposed;

    /// <summary>Records the name once; later calls do nothing; returns a faulted task when a failure was supplied.</summary>
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
