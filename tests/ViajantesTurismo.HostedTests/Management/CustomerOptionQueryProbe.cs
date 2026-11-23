using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Records SQL reads and row limits for the real customer option query.</summary>
internal sealed class CustomerOptionQueryProbe : DbCommandInterceptor
{
    private int _reads;
    private int _limitedReads;

    /// <summary>Gets the number of read commands against the customer tables.</summary>
    internal int Reads => Volatile.Read(ref _reads);

    /// <summary>Gets the number of customer read commands with a database limit.</summary>
    internal int LimitedReads => Volatile.Read(ref _limitedReads);

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("\"Customers\"", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _reads);
            if (command.CommandText.Contains("LIMIT", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _limitedReads);
            }
        }

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
