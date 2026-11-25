using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Retires a tour on a second connection before executing the already counted page read.</summary>
internal sealed class CatalogPublicTourPageShrink(Func<CancellationToken, Task<int>> retire)
    : DbCommandInterceptor
{
    /// <summary>Gets the number of retirement markers written once the page read starts.</summary>
    internal int? RetiredRows
    {
        get;
        private set;
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (RetiredRows is null && command.CommandText.Contains("OFFSET", StringComparison.Ordinal)
            && command.CommandText.Contains("\"CatalogTourReadModels\"", StringComparison.Ordinal))
        {
            RetiredRows = await retire(cancellationToken);
        }

        return result;
    }
}
