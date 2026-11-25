using Npgsql;
using NpgsqlTypes;
using SharedKernel.AspNetCore.Authentication.Bff;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.Management.Web;

/// <summary>Compares exact protected values in the PostgreSQL cache before replacing or removing a token entry.</summary>
/// <param name="dataSource">The dedicated data source for conditional token cache mutations.</param>
internal sealed class PostgreSqlUserTokenCacheConcurrency(NpgsqlDataSource dataSource) : IUserTokenCacheConcurrency
{
    private const string CacheTable = $"{ManagementSecurityDefaults.SchemaName}.{ManagementSecurityDefaults.TicketTableName}";

    // Lock the matched entry before checking expiration so a wait cannot revive an elapsed entry.
    private const string ReplaceSql = $"""
        WITH expected_entry AS MATERIALIZED (
            SELECT id, expiresattime
            FROM {CacheTable}
            WHERE id = $1 AND value = $2
            FOR NO KEY UPDATE
        )
        UPDATE {CacheTable} AS cache
        SET value = $3,
            expiresattime = $4,
            absoluteexpiration = $4,
            slidingexpirationinseconds = NULL
        FROM expected_entry
        WHERE cache.id = expected_entry.id
          AND LEAST(expected_entry.expiresattime, $4) > pg_catalog.clock_timestamp();
        """;
    private const string RemoveSql = $"""
        DELETE FROM {CacheTable}
        WHERE id = $1 AND value = $2;
        """;

    /// <inheritdoc />
    public async Task<bool> TryReplace(
        string key,
        ReadOnlyMemory<byte> expectedValue,
        ReadOnlyMemory<byte> replacement,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(ReplaceSql);
        command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = key });
        command.Parameters.Add(new NpgsqlParameter<ReadOnlyMemory<byte>> { NpgsqlDbType = NpgsqlDbType.Bytea, TypedValue = expectedValue });
        command.Parameters.Add(new NpgsqlParameter<ReadOnlyMemory<byte>> { NpgsqlDbType = NpgsqlDbType.Bytea, TypedValue = replacement });
        command.Parameters.Add(new NpgsqlParameter<DateTimeOffset> { TypedValue = expiresAt.ToUniversalTime() });
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    /// <inheritdoc />
    public async Task<bool> TryRemove(string key, ReadOnlyMemory<byte> expectedValue, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(RemoveSql);
        command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = key });
        command.Parameters.Add(new NpgsqlParameter<ReadOnlyMemory<byte>> { NpgsqlDbType = NpgsqlDbType.Bytea, TypedValue = expectedValue });
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }
}
