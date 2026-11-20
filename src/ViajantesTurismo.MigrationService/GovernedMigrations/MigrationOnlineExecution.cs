using System.Globalization;
using Npgsql;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Runs a declared index operation on the owner session and checks its catalog state.</summary>
internal static class MigrationOnlineExecution
{
    /// <summary>Separates missing, matching, invalid, and conflicting catalog objects.</summary>
    internal static async Task<MigrationOnlineIndexState> Inspect(
        NpgsqlConnection verifier, MigrationStep step, MigrationOnlineIndex index, CancellationToken ct)
    {
        const string query = """
            SELECT CASE
                WHEN relation.relkind <> 'i' OR definition.indexrelid IS NULL OR EXISTS (
                    SELECT 1 FROM pg_catalog.pg_constraint AS constraint_definition
                    WHERE constraint_definition.conindid = relation.oid
                      AND constraint_definition.contype IN ('p', 'u', 'x')) THEN 'conflict'
                WHEN pg_catalog.pg_get_indexdef(relation.oid) IS DISTINCT FROM pg_catalog.format(
                    'CREATE %sINDEX %I ON %I.%I USING btree (%s)',
                    CASE WHEN $4 THEN 'UNIQUE ' ELSE '' END, $2, $1, $3,
                    (SELECT pg_catalog.string_agg(pg_catalog.quote_ident(key.name), ', ' ORDER BY key.position)
                     FROM pg_catalog.unnest($5) WITH ORDINALITY AS key(name, position))) THEN 'conflict'
                WHEN definition.indislive AND definition.indisready AND definition.indisvalid THEN 'ready'
                ELSE 'invalid'
            END
            FROM pg_catalog.pg_class AS relation
            JOIN pg_catalog.pg_namespace AS schema ON schema.oid = relation.relnamespace
            LEFT JOIN pg_catalog.pg_index AS definition ON definition.indexrelid = relation.oid
            WHERE schema.nspname = $1 AND relation.relname = $2;
            """;
        await using var transaction = await verifier.BeginTransactionAsync(ct);
        await using var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL search_path TO pg_catalog;", verifier, transaction);
        await readOnly.ExecuteNonQueryAsync(ct);
        await MigrationPostgreSql.SetTimeouts(verifier, transaction, step, ct);
        await using var command = new NpgsqlCommand(query, verifier, transaction) { CommandTimeout = step.TimeoutSeconds };
        MigrationPostgreSql.Bind(command, index.Schema, index.Name, index.Table, index.Unique, index.Columns);
        var result = await command.ExecuteScalarAsync(ct);
        await transaction.CommitAsync(ct);
        return result switch
        {
            null => MigrationOnlineIndexState.Missing,
            "ready" => MigrationOnlineIndexState.Ready,
            "invalid" => MigrationOnlineIndexState.Invalid,
            "conflict" => MigrationOnlineIndexState.Conflict,
            _ => throw new InvalidDataException("Unexpected online index catalog state.")
        };
    }

    /// <summary>Dispatches missing index creation and verifies the exact observable result.</summary>
    internal static async Task<string?> Run(NpgsqlConnection owner, NpgsqlConnection verifier, MigrationStep step,
        MigrationOnlineIndex index, MigrationOnlineIndexState state, string sql, string verification, CancellationToken ct)
    {
        if (state == MigrationOnlineIndexState.Missing)
        {
            await Create(owner, step, sql, ct);
        }

        if (await Inspect(verifier, step, index, ct) != MigrationOnlineIndexState.Ready)
        {
            return "online_index_unverified";
        }

        return await MigrationPostgreSql.Verify(verifier, step, verification, ct) ? null : "verification_failed";
    }

    private static async Task Create(NpgsqlConnection owner, MigrationStep step, string sql, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT pg_catalog.current_setting('statement_timeout'), pg_catalog.current_setting('lock_timeout');
            """, owner);
        string priorStatement;
        string priorLock;
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct))
            {
                throw new InvalidDataException("Cannot read online migration timeouts.");
            }

            priorStatement = reader.GetString(0);
            priorLock = reader.GetString(1);
        }

        await SetTimeouts(owner, step.TimeoutSeconds.ToString(CultureInfo.InvariantCulture) + "s",
            step.LockTimeoutSeconds.ToString(CultureInfo.InvariantCulture) + "s", ct);

        // One parameterless batch command uses a single extended-protocol Parse.
        // Allow two seconds for the server's timeout response before the client cancels.
        await using var batch = new NpgsqlBatch(owner) { BatchCommands = { new NpgsqlBatchCommand(sql) }, Timeout = step.TimeoutSeconds + 2 };
        await batch.ExecuteNonQueryAsync(ct);
        await SetTimeouts(owner, priorStatement, priorLock, ct);
    }

    private static async Task SetTimeouts(NpgsqlConnection owner, string statement, string acquisition, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT pg_catalog.set_config('statement_timeout', $1, false),
                   pg_catalog.set_config('lock_timeout', $2, false);
            """, owner);
        MigrationPostgreSql.Bind(command, statement, acquisition);
        await command.ExecuteNonQueryAsync(ct);
    }
}
