using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using SharedKernel.Npgsql;
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
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(lockIdentity));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
