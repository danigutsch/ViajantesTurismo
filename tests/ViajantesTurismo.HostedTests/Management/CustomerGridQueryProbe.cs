using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Checks the PostgreSQL readers used for customer page projections.</summary>
internal sealed class CustomerGridQueryProbe : DbCommandInterceptor
{
    private static readonly string[] SummaryColumns = ["Id", "FirstName", "LastName", "Email", "Mobile", "Nationality"];
    private int _limitedReads;
    private int _summaryReads;

    /// <summary>Gets the customer queries that specified a row limit.</summary>
    internal int LimitedReads => Volatile.Read(ref _limitedReads);

    /// <summary>Gets the limited queries that returned exactly the overview fields.</summary>
    internal int SummaryReads => Volatile.Read(ref _summaryReads);

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("\"Customers\"", StringComparison.Ordinal) &&
            command.CommandText.Contains("LIMIT", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _limitedReads);
            var columns = Enumerable.Range(0, result.FieldCount).Select(result.GetName);
            if (columns.SequenceEqual(SummaryColumns, StringComparer.Ordinal))
            {
                Interlocked.Increment(ref _summaryReads);
            }
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
