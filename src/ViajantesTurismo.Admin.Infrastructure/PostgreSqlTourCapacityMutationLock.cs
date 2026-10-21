using Npgsql;
using PandaBear.Npgsql;
using ViajantesTurismo.Admin.Application.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class PostgreSqlTourCapacityMutationLock(NpgsqlDataSource dataSource) : ITourCapacityMutationLock
{
    private const string LockPurpose = "admin:tour-capacity:";

    public ValueTask<IAsyncDisposable> Acquire(Guid tourId, CancellationToken ct) =>
        PostgreSqlSessionAdvisoryLock.Acquire(dataSource, CreateLockKey(tourId), ct);

    private static long CreateLockKey(Guid tourId)
    {
        var lockIdentity = string.Concat(LockPurpose, tourId.ToString("D", System.Globalization.CultureInfo.InvariantCulture));
        return PostgreSqlAdvisoryLockKey.FromUtf8Sha256BigEndian(lockIdentity);
    }
}
