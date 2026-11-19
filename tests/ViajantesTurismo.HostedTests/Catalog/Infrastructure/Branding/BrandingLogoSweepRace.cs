using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Branding;

/// <summary>Runs a competing collector on either side of acquiring the writer's logo lock.</summary>
internal sealed class BrandingLogoSweepRace(bool afterReferenceLock, Func<CancellationToken, Task<int>> reclaim) : DbCommandInterceptor
{
    /// <summary>Gets how many rows the concurrent sweep removed, or -1 until it runs.</summary>
    internal int RemovedRows { get; private set; } = -1;

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (!afterReferenceLock && RemovedRows < 0 && command.CommandText.Contains("ON CONFLICT", StringComparison.Ordinal))
        {
            RemovedRows = await reclaim(cancellationToken);
        }

        return result;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        if (afterReferenceLock && RemovedRows < 0 && command.CommandText.Contains("FOR KEY SHARE", StringComparison.Ordinal))
        {
            RemovedRows = await reclaim(cancellationToken);
        }

        return result;
    }
}
