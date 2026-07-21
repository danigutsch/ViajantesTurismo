using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using SharedKernel.Npgsql;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class PostgreSqlCatalogTourSlugLock(NpgsqlDataSource dataSource) : ICatalogTourSlugLock
{
    public ValueTask<IAsyncDisposable> Acquire(string slug, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return PostgreSqlSessionAdvisoryLock.Acquire(dataSource, CreateLockKey(slug), ct);
    }

    private static long CreateLockKey(string slug)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(slug));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }

}
