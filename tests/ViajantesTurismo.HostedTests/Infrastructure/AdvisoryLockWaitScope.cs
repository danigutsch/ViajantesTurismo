namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>Releases a held test operation and observes the operation waiting behind it.</summary>
internal sealed class AdvisoryLockWaitScope(Func<ValueTask> release, Func<ValueTask> complete) : IAsyncDisposable
{
    /// <summary>Releases the held operation and then awaits completion of the waiting operation.</summary>
    public async ValueTask DisposeAsync()
    {
        await release();
        await complete();
    }
}
