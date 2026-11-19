using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.CommitAmbiguity;

/// <summary>Throws one retryable timeout after another connection has observed the committed identifiers.</summary>
internal sealed class CommittedTimeoutInterceptor(
    DbContextOptions<AdminWriteDbContext> observerOptions,
    Func<AdminWriteDbContext, IQueryable<Guid>> selectIds)
    : DbTransactionInterceptor
{
    private bool armed;

    /// <summary>Gets the identifiers verified on a separate connection before the timeout.</summary>
    internal Guid[] CommittedIds { get; private set; } = [];

    /// <summary>Arms the commit of the command transaction after setup has completed.</summary>
    internal void Arm()
    {
        armed = true;
    }

    /// <inheritdoc />
    public override async Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (!armed)
        {
            return;
        }

        armed = false;
        CommittedIds = await ReadIds(cancellationToken);
        throw new TimeoutException("Test-owned loss of the command commit acknowledgment.");
    }

    /// <summary>Reads committed identifiers on a separate connection.</summary>
    internal async Task<Guid[]> ReadIds(CancellationToken ct)
    {
        await using var reader = new AdminWriteDbContext(observerOptions);
        return await selectIds(reader).ToArrayAsync(ct);
    }
}
