namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>Releases a held test operation and observes the operation waiting behind it.</summary>
internal sealed class AdvisoryLockWaitScope(Func<ValueTask> release, Func<ValueTask> complete) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await release();
        await complete();
    }
}
