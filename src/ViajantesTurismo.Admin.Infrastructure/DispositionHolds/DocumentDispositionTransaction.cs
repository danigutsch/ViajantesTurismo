using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using PandaBear.Npgsql;
using ViajantesTurismo.Admin.Application.Documents.Holds;

namespace ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

/// <summary>
/// Runs a document disposition or hold change in one explicit read committed transaction under the context's
/// execution strategy. A transient failure re-runs the whole unit after the change tracker is cleared.
/// </summary>
/// <param name="context">The Admin write database context.</param>
internal sealed class DocumentDispositionTransaction(AdminWriteDbContext context) : IDocumentDispositionTransaction
{
    /// <inheritdoc />
    public async Task<T> Execute<T>(IReadOnlyList<string> lockNames, Func<Task<T>> unit, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lockNames);
        ArgumentNullException.ThrowIfNull(unit);

        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
            foreach (var key in lockNames.Select(PostgreSqlAdvisoryLockKey.FromUtf8Sha256BigEndian).Distinct().Order())
            {
                await PostgreSqlTransactionAdvisoryLock.Acquire(
                    (NpgsqlConnection)context.Database.GetDbConnection(),
                    (NpgsqlTransaction)transaction.GetDbTransaction(),
                    key,
                    ct);
            }

            var result = await unit();
            await transaction.CommitAsync(ct);
            return result;
        });
    }
}
