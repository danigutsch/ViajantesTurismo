using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

/// <summary>Holds each save until two have arrived and <see cref="Release" /> is called, forcing concurrent saves.</summary>
internal sealed class DocumentSaveBarrierInterceptor : SaveChangesInterceptor
{
    private readonly TaskCompletionSource bothSaving = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int arrivals;

    /// <summary>Completes when two saves are waiting at the barrier.</summary>
    public Task BothSaving => bothSaving.Task;

    /// <summary>Lets the blocked saves proceed.</summary>
    public void Release() => release.TrySetResult();

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct)
    {
        if (Interlocked.Increment(ref arrivals) == 2)
        {
            bothSaving.TrySetResult();
        }

        await release.Task.WaitAsync(ct);
        return result;
    }
}
