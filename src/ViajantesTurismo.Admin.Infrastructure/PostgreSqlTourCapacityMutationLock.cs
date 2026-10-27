using Npgsql;
using PandaBear.Npgsql;
using ViajantesTurismo.Admin.Application.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>Serializes tour capacity mutations with a PostgreSQL session advisory lock per tour.</summary>
/// <param name="dataSource">The PostgreSQL data source used to open the lock session.</param>
internal sealed class PostgreSqlTourCapacityMutationLock(NpgsqlDataSource dataSource) : ITourCapacityMutationLock
{
    private const string LockPurpose = "admin:tour-capacity:";

    /// <inheritdoc />
    public ValueTask<IAsyncDisposable> Acquire(Guid tourId, CancellationToken ct) =>
        PostgreSqlSessionAdvisoryLock.Acquire(dataSource, CreateLockKey(tourId), ct);

    private static long CreateLockKey(Guid tourId)
    {
        var lockIdentity = string.Concat(LockPurpose, tourId.ToString("D", System.Globalization.CultureInfo.InvariantCulture));
        return PostgreSqlAdvisoryLockKey.FromUtf8Sha256BigEndian(lockIdentity);
    }
}
