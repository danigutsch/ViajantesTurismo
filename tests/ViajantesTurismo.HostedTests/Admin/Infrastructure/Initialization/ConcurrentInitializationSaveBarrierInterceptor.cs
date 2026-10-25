using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Initialization;

internal sealed class ConcurrentInitializationSaveBarrierInterceptor : SaveChangesInterceptor
{
    private readonly TaskCompletionSource bothSaving = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int arrivals;

    public Task BothSaving => bothSaving.Task;

    public void Release() => release.TrySetResult();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref arrivals) == 2)
        {
            bothSaving.TrySetResult();
        }

        await release.Task.WaitAsync(cancellationToken);
        return result;
    }
}
