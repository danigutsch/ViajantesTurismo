using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Observes tour query bounds and aggregate materialization in the real Admin read context.</summary>
internal sealed class TourGridQueryProbe : DbCommandInterceptor, IMaterializationInterceptor
{
    private static readonly string[] OverviewFields =
    [
        "Id", "Identifier", "Name", "StartDate", "EndDate", "BasePrice", "SingleRoomSupplementPrice",
        "RegularBikePrice", "EBikePrice", "Currency", "MinCustomers", "MaxCustomers", "CurrentCustomerCount"
    ];

    private int _tourReads;
    private int _limitedTourReads;
    private int _overviewReads;
    private int _materializedTours;
    private string? _lastLimitedProjection;

    /// <summary>Gets the number of reads against the tours table.</summary>
    internal int TourReads => Volatile.Read(ref _tourReads);

    /// <summary>Gets the number of tour reads that apply a row limit in SQL.</summary>
    internal int LimitedTourReads => Volatile.Read(ref _limitedTourReads);

    /// <summary>Gets the limited reads that return the overview fields and one scalar confirmed count.</summary>
    internal int OverviewReads => Volatile.Read(ref _overviewReads);

    /// <summary>Gets the fields in the last limited response for assertion diagnostics.</summary>
    internal string? LastLimitedProjection => Volatile.Read(ref _lastLimitedProjection);

    /// <summary>Gets the number of full tour aggregates materialized in EF Core.</summary>
    internal int MaterializedTours => Volatile.Read(ref _materializedTours);

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("\"Tours\"", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _tourReads);
            if (command.CommandText.Contains("LIMIT", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _limitedTourReads);
                var columns = Enumerable.Range(0, result.FieldCount).Select(result.GetName).ToHashSet(StringComparer.Ordinal);
                Volatile.Write(ref _lastLimitedProjection, $"{result.FieldCount} fields: {string.Join(", ", columns)}");
                if (result.FieldCount == OverviewFields.Length && columns.SetEquals(OverviewFields))
                {
                    Interlocked.Increment(ref _overviewReads);
                }
            }
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
    {
        if (entity is Tour)
        {
            Interlocked.Increment(ref _materializedTours);
        }

        return entity;
    }
}
