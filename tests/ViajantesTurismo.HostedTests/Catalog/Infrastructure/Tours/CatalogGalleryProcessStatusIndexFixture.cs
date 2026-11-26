using Npgsql;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Inspects the Gallery status indexes in the isolated Catalog test database.</summary>
internal static class CatalogGalleryProcessStatusIndexFixture
{
    /// <summary>Counts the valid Gallery indexes in the event store schema.</summary>
    internal static async Task<int> CountValidIndexes(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT count(*)::integer
            FROM pg_catalog.pg_index AS index_definition
            JOIN pg_catalog.pg_class AS relation ON relation.oid = index_definition.indexrelid
            JOIN pg_catalog.pg_namespace AS schema ON schema.oid = relation.relnamespace
            WHERE schema.nspname = 'event_sourcing'
              AND relation.relname IN (@outcome, @retirement)
              AND index_definition.indisvalid AND index_definition.indisready
            """);
        command.Parameters.AddWithValue("outcome", CatalogGalleryProcessStatusIndexes.OutcomeIndexName);
        command.Parameters.AddWithValue("retirement", CatalogGalleryProcessStatusIndexes.RetirementIndexName);
        return (int)(await command.ExecuteScalarAsync(ct) ?? throw new InvalidDataException("The index count is unavailable."));
    }

    /// <summary>Cancels the initializer after a competing writer blocks its first build.</summary>
    internal static async Task<bool> CancelBlockedBuild(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var observation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        observation.CancelAfter(TimeSpan.FromSeconds(10));
        var initialize = CatalogGalleryProcessStatusIndexes.EnsureIndexes(dataSource, cancellation.Token);
        var cancelled = false;
        try
        {
            await WaitForBlockedBuild(dataSource, observation.Token);
        }
        finally
        {
            await cancellation.CancelAsync();
            try
            {
                await initialize;
            }
            catch (OperationCanceledException exception)
            {
                cancelled = exception.CancellationToken == cancellation.Token;
            }
        }

        return cancelled;
    }

    private static async Task WaitForBlockedBuild(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("""
            SELECT EXISTS (
                SELECT 1 FROM pg_catalog.pg_stat_activity
                WHERE datname = current_database()
                  AND wait_event_type = 'Lock'
                  AND query LIKE 'CREATE INDEX%'
            )
            """);

        while (await command.ExecuteScalarAsync(ct) is not true)
        {
            ct.ThrowIfCancellationRequested();
        }
    }
}
