using System.Globalization;
using Npgsql;
using PandaBear.Npgsql;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class PostgreSqlCatalogTourMutationLock(NpgsqlDataSource dataSource) : ICatalogTourMutationLock
{
    public async ValueTask<IAsyncDisposable> Acquire(IReadOnlyCollection<Guid> catalogTourIds, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(catalogTourIds);
        var connection = await dataSource.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
            foreach (var id in catalogTourIds.Distinct().Order())
            {
                var key = PostgreSqlAdvisoryLockKey.FromUtf8Sha256BigEndian(
                    string.Create(CultureInfo.InvariantCulture, $"catalog-tour-mutation:{id:N}"));
                await PostgreSqlTransactionAdvisoryLock.Acquire(connection, transaction, key, ct).ConfigureAwait(false);
            }

            return new Lease(connection, transaction);
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private sealed class Lease(NpgsqlConnection connection, NpgsqlTransaction transaction) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
