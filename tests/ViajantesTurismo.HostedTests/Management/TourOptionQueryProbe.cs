using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Records SQL reads, row limits, and booking joins for tour option requests.</summary>
internal sealed class TourOptionQueryProbe : DbCommandInterceptor
{
    private static readonly string[] ChoiceFields = ["Id", "Name", "Identifier", "StartDate"];

    private int reads;
    private int limitedReads;
    private int choiceProjectionReads;
    private int bookingReads;

    /// <summary>Gets the number of read commands against the tour table.</summary>
    internal int Reads => Volatile.Read(ref reads);

    /// <summary>Gets the number of tour read commands that limit their results in PostgreSQL.</summary>
    internal int LimitedReads => Volatile.Read(ref limitedReads);

    /// <summary>Gets the number of limited reads that select exactly the tour-choice fields.</summary>
    internal int ChoiceProjectionReads => Volatile.Read(ref choiceProjectionReads);

    /// <summary>Gets the number of tour read commands that include the booking table.</summary>
    internal int BookingReads => Volatile.Read(ref bookingReads);

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("\"Tours\"", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref reads);
            if (command.CommandText.Contains("LIMIT", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref limitedReads);
                if (Enumerable.Range(0, result.FieldCount).Select(result.GetName).SequenceEqual(ChoiceFields))
                {
                    Interlocked.Increment(ref choiceProjectionReads);
                }
            }

            if (command.CommandText.Contains("\"Booking\"", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref bookingReads);
            }
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
