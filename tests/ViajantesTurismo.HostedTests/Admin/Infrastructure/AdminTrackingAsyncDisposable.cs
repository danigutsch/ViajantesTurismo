namespace ViajantesTurismo.Admin.Infrastructure.Tests;

internal sealed class AdminTrackingAsyncDisposable(
    string name,
    ICollection<string> disposalOrder,
    Exception? failure = null) : IAsyncDisposable
{
    private int disposed;

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
