using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Branding;

/// <summary>Commits the competing logo just before the first writer's insert reaches PostgreSQL.</summary>
internal sealed class BrandingLogoInsertRace(Func<CancellationToken, Task> insertCompetingLogo) : DbCommandInterceptor
{
    private int injected;

    /// <summary>Gets whether the competing writer reached the insertion boundary.</summary>
    internal bool WasInjected => Volatile.Read(ref injected) == 1;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        await InsertCompetitor(command, cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await InsertCompetitor(command, cancellationToken);
        return result;
    }

    private async Task InsertCompetitor(DbCommand command, CancellationToken ct)
    {
        if (command.CommandText.Contains("INSERT INTO branding.\"BrandingLogos\"", StringComparison.Ordinal)
            && Interlocked.Exchange(ref injected, 1) == 0)
        {
            await insertCompetingLogo(ct);
        }
    }
}
