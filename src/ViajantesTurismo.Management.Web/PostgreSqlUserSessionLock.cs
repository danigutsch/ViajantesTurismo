using Npgsql;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.Npgsql;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Serializes token refresh, exchange and sign-out of one session across every Management Web instance with a PostgreSQL
/// transaction advisory lock on the security database.
/// </summary>
/// <param name="dataSource">The data source of the security database, named for pool metrics.</param>
internal sealed class PostgreSqlUserSessionLock(NpgsqlDataSource dataSource) : IUserSessionLock
{
    private const string LockKeyPurpose = "ViajantesTurismo.Management.Web.UserSession:";

    /// <inheritdoc />
    public async ValueTask<IAsyncDisposable> Acquire(string sessionId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var connection = await dataSource.OpenConnectionAsync(ct);
        NpgsqlTransaction? transaction = null;
        try
        {
            transaction = await connection.BeginTransactionAsync(ct);
            await PostgreSqlTransactionAdvisoryLock.AcquireKey(connection, transaction, LockKey(sessionId), ct);
            return new Lease(connection, transaction);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }

            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>Derives the advisory lock key of a session.</summary>
    internal static PostgreSqlLockKey LockKey(string sessionId) => PostgreSqlLockKey.FromIdentity(LockKeyPurpose + sessionId);

    private sealed class Lease(NpgsqlConnection connection, NpgsqlTransaction transaction) : IAsyncDisposable
    {
        private int _released;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return;
            }

            await transaction.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
