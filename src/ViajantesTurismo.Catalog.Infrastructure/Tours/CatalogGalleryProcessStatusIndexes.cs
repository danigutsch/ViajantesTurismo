using Npgsql;
using SharedKernel.Npgsql;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Installs and verifies app-owned Gallery status indexes for local Catalog databases.</summary>
internal static class CatalogGalleryProcessStatusIndexes
{
    /// <summary>The Catalog event-store schema initialized by the provider.</summary>
    internal const string EventSchema = "event_sourcing";

    /// <summary>The index that locates a committed outcome by stream and intent identity.</summary>
    internal const string OutcomeIndexName = "ix_catalog_events_gallery_intent_outcome";

    /// <summary>The index that locates a committed retirement by stream.</summary>
    internal const string RetirementIndexName = "ix_catalog_events_tour_retirement";

    private const string Apply = CatalogEventSerializer.CatalogTourGalleryChangedEventType;
    private const string Reject = CatalogEventSerializer.CatalogTourGalleryIntentRejectedEventType;
    private const string Retire = CatalogEventSerializer.CatalogTourRetiredEventType;

    private const string OutcomeDefinition =
        $"CREATE INDEX {OutcomeIndexName} ON {EventSchema}.events USING btree " +
        "(stream_id, ((payload_json ->> 'IntentId'::text)), ((payload_json ->> 'IntentSequence'::text)), stream_revision) " +
        $"INCLUDE (event_type, recorded_at_utc) WHERE (event_type = ANY (ARRAY['{Apply}'::text, '{Reject}'::text]))";
    private const string RetirementDefinition =
        $"CREATE INDEX {RetirementIndexName} ON {EventSchema}.events USING btree (stream_id, stream_revision) " +
        $"INCLUDE (recorded_at_utc) WHERE (event_type = '{Retire}'::text)";

    private const string ReadDefinition = """
        SELECT CASE WHEN definition.indislive AND definition.indisready AND definition.indisvalid
                    THEN pg_catalog.pg_get_indexdef(relation.oid) END
        FROM pg_catalog.pg_class AS relation
        JOIN pg_catalog.pg_namespace AS schema ON schema.oid = relation.relnamespace
        LEFT JOIN pg_catalog.pg_index AS definition ON definition.indexrelid = relation.oid
        WHERE schema.nspname = @schema AND relation.relname = @name
        """;

    private static readonly long InitializationLock =
        PostgreSqlAdvisoryLockKey.FromUtf8Sha256BigEndian("catalog-gallery-process-status-index-initialization");

    /// <summary>Ensures matching indexes after the event tables have been initialized.</summary>
    internal static async Task EnsureIndexes(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        var connection = await dataSource.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var ownedConnection = connection.ConfigureAwait(false);
        var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using var ownedTransaction = transaction.ConfigureAwait(false);
        await PostgreSqlTransactionAdvisoryLock.Acquire(connection, transaction, InitializationLock, ct).ConfigureAwait(false);
        // Use the same search path as governed catalog verification so PostgreSQL qualifies the event table consistently.
        var searchPath = new NpgsqlCommand("SET LOCAL search_path TO pg_catalog", connection, transaction);
        await using var ownedSearchPath = searchPath.ConfigureAwait(false);
        await searchPath.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        var outcome = new NpgsqlCommand(OutcomeDefinition, connection, transaction);
        await using var ownedOutcome = outcome.ConfigureAwait(false);
        var retirement = new NpgsqlCommand(RetirementDefinition, connection, transaction);
        await using var ownedRetirement = retirement.ConfigureAwait(false);
        await EnsureIndex(connection, transaction, OutcomeIndexName, outcome, ct).ConfigureAwait(false);
        await EnsureIndex(connection, transaction, RetirementIndexName, retirement, ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }

    private static async Task EnsureIndex(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string indexName, NpgsqlCommand install, CancellationToken ct)
    {
        var inspect = new NpgsqlCommand(ReadDefinition, connection, transaction);
        await using var ownedInspect = inspect.ConfigureAwait(false);
        inspect.Parameters.AddWithValue("schema", EventSchema);
        inspect.Parameters.AddWithValue("name", indexName);
        var definition = await inspect.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (definition is null)
        {
            await install.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            definition = await inspect.ExecuteScalarAsync(ct).ConfigureAwait(false);
        }

        if (!string.Equals(definition as string, install.CommandText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Catalog Gallery status index '{indexName}' is invalid or has an unexpected definition.");
        }
    }
}
