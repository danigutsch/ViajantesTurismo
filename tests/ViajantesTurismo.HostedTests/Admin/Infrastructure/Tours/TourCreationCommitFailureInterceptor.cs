using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Raises one PostgreSQL serialization failure just before the armed transaction commits.</summary>
internal sealed class TourCreationCommitFailureInterceptor : DbTransactionInterceptor
{
    private bool armed;

    /// <summary>The number of injected serialization failures.</summary>
    internal int Failures
    {
        get;
        private set;
    }

    /// <summary>Arms the next commit after the database has been migrated.</summary>
    internal void Arm()
    {
        armed = true;
    }

    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(
        DbTransaction transaction,
        TransactionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        if (!armed)
        {
            return result;
        }

        armed = false;
        var connection = transaction.Connection ?? throw new InvalidOperationException("Expected an active PostgreSQL transaction.");
        await using var fault = connection.CreateCommand();
        fault.Transaction = transaction;
        fault.CommandText = """
            DO $$
            BEGIN
                RAISE EXCEPTION USING ERRCODE = '40001', MESSAGE = 'test-owned serialization failure';
            END;
            $$;
            """;
        Failures++;
        _ = await fault.ExecuteNonQueryAsync(cancellationToken);
        return result;
    }
}
