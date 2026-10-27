namespace ViajantesTurismo.Admin.Infrastructure.Tests;

/// <summary>Async-disposable probe that records the order in which instances are disposed and can simulate a disposal failure.</summary>
/// <param name="name">Name appended to the disposal order when this instance is first disposed.</param>
/// <param name="disposalOrder">Collection that receives the names in disposal order.</param>
/// <param name="failure">Exception returned from the first disposal, or <see langword="null" /> to complete successfully.</param>
internal sealed class AdminTrackingAsyncDisposable(
    string name,
    ICollection<string> disposalOrder,
    Exception? failure = null) : IAsyncDisposable
{
    private int disposed;

    /// <summary>Records this instance's name once in the disposal order, then faults if a failure was supplied; later calls are no-ops.</summary>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        disposalOrder.Add(name);
        return failure is null
            ? ValueTask.CompletedTask
            : ValueTask.FromException(failure);
    }
}
