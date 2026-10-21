using Npgsql;
using PandaBear.Npgsql;
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
        return PostgreSqlAdvisoryLockKey.FromUtf8Sha256BigEndian(slug);
    }

}
