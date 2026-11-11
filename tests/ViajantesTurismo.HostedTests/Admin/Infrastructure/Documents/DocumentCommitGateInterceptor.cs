using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

/// <summary>Holds a save's transaction just before commit, with its statements executed and row locks held, until <see cref="Release" /> is called.</summary>
internal sealed class DocumentCommitGateInterceptor : DbTransactionInterceptor
{
    private readonly TaskCompletionSource readyToCommit = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes when the transaction has executed its statements and is about to commit.</summary>
    public Task ReadyToCommit => readyToCommit.Task;

    /// <summary>Lets the held transaction commit.</summary>
    public void Release() => release.TrySetResult();

    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        readyToCommit.TrySetResult();
        await release.Task.WaitAsync(cancellationToken);
        return result;
    }
}
