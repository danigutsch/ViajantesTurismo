using Npgsql;
using PandaBear.Npgsql;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Serializes work on a tour slug with a PostgreSQL session advisory lock.</summary>
internal sealed class PostgreSqlCatalogTourSlugLock(NpgsqlDataSource dataSource) : ICatalogTourSlugLock
{
    /// <summary>Acquires the advisory lock for the slug and returns a lease that releases it on disposal.</summary>
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
