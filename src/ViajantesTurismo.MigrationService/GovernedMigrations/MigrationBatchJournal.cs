using System.Globalization;
using Npgsql;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Stores durable batch checkpoints without giving the data login direct journal writes.</summary>
internal sealed class MigrationBatchJournal(NpgsqlConnection owner, MigrationManifest manifest, MigrationJournalSchema journalSchema)
{
    private string Schema => journalSchema.Name;
    private string Attempts => journalSchema.Attempts;
    private string Batches => Schema + ".batches";
    private string Authorizations => Schema + ".batch_authorizations";
    private string CheckpointFunction => Schema + ".checkpoint_batch";
    private string CheckpointSignature => CheckpointFunction + "(bigint, uuid, bigint, bigint, integer, boolean)";
    private const string BatchLimitSql = "1000";

    /// <summary>The maximum number of rows approved for one transaction.</summary>
    internal static readonly int BatchLimit = int.Parse(BatchLimitSql, CultureInfo.InvariantCulture);

    /// <summary>Creates the checkpoint tables and gives the selected login access to the guarded write function.</summary>
    internal async Task Prepare(string dataLogin, CancellationToken ct)
    {
        await using var transaction = await owner.BeginTransactionAsync(ct);
        await using var schema = new NpgsqlBatch(owner, transaction)
        {
            BatchCommands =
            {
                new NpgsqlBatchCommand($"""
                    DO $journal$
                    BEGIN
                        IF EXISTS (
                            SELECT 1 FROM pg_catalog.pg_constraint
                            WHERE conrelid = '{Attempts}'::pg_catalog.regclass AND conname = 'attempts_phase_check'
                        ) THEN
                            ALTER TABLE {Attempts} DROP CONSTRAINT attempts_phase_check,
                                ADD CONSTRAINT migration_attempt_phase CHECK (phase IN ('expand', 'data', 'verify'));
                        END IF;
                    END;
                    $journal$;
                    """),
                new NpgsqlBatchCommand($"""
                    CREATE TABLE IF NOT EXISTS {Authorizations} (
                        attempt_id bigint PRIMARY KEY REFERENCES {Attempts},
                        checkpoint_key text NOT NULL,
                        verification_sha256 text NOT NULL CHECK (length(verification_sha256) = 64),
                        data_role name NOT NULL,
                        proof_hash bytea NOT NULL CHECK (length(proof_hash) = 32)
                    );
                    """),
                new NpgsqlBatchCommand($"""
                    CREATE TABLE IF NOT EXISTS {Batches} (
                        batch_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        attempt_id bigint NOT NULL REFERENCES {Attempts},
                        checkpoint_key text NOT NULL,
                        source_cursor bigint NOT NULL CHECK (source_cursor >= 0),
                        next_cursor bigint NOT NULL,
                        affected_count integer NOT NULL CHECK (affected_count BETWEEN 0 AND {BatchLimitSql}),
                        finished boolean NOT NULL,
                        committed_at timestamptz NOT NULL DEFAULT pg_catalog.clock_timestamp(),
                        UNIQUE (attempt_id, source_cursor),
                        CHECK ((affected_count > 0 AND next_cursor > source_cursor)
                            OR (finished AND affected_count = 0 AND next_cursor = source_cursor))
                    );
                    """),
                new NpgsqlBatchCommand($"""
                    CREATE OR REPLACE FUNCTION {CheckpointFunction}(
                        run_attempt bigint, proof uuid, input_cursor bigint, output_cursor bigint, changed integer, done boolean)
                    RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, pg_temp AS $checkpoint$
                    DECLARE
                        progress_key text;
                    BEGIN
                        SELECT auth.checkpoint_key INTO progress_key
                        FROM {Authorizations} AS auth INNER JOIN {Attempts} AS a USING (attempt_id)
                        WHERE a.attempt_id = run_attempt AND a.phase = 'data' AND a.state = 'started'
                          AND auth.data_role = session_user
                          AND auth.proof_hash = pg_catalog.sha256(pg_catalog.convert_to(proof::text, 'UTF8'));

                        IF NOT FOUND THEN
                            RAISE EXCEPTION USING ERRCODE = '42501', MESSAGE = 'Data batch attempt is not authorized.';
                        END IF;

                        IF output_cursor IS NOT NULL THEN
                            INSERT INTO {Batches} (attempt_id, checkpoint_key, source_cursor, next_cursor, affected_count, finished)
                            VALUES (run_attempt, progress_key, input_cursor, output_cursor, changed, done);
                        END IF;
                    END;
                    $checkpoint$;
                    """),
                new NpgsqlBatchCommand($"""
                    REVOKE ALL ON FUNCTION {CheckpointSignature} FROM PUBLIC;
                    """),
                new NpgsqlBatchCommand($"""
                    CREATE FUNCTION pg_temp.grant_migration_batch_checkpoint(data_role text)
                    RETURNS void LANGUAGE plpgsql SECURITY INVOKER SET search_path = pg_catalog, pg_temp AS $grant$
                    BEGIN
                        EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA {Schema} TO %I', data_role);
                        EXECUTE pg_catalog.format('GRANT EXECUTE ON FUNCTION {CheckpointSignature} TO %I', data_role);
                    END;
                    $grant$;
                    """),
            }
        };
        await schema.ExecuteNonQueryAsync(ct);

        await using var grant = new NpgsqlCommand("SELECT pg_temp.grant_migration_batch_checkpoint($1);", owner, transaction);
        MigrationPostgreSql.Bind(grant, dataLogin);
        await grant.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }

    /// <summary>Authorizes a single data attempt without revealing journal contents or storing its proof.</summary>
    internal async Task<Guid> Authorize(long attempt, MigrationStep step, string dataLogin, CancellationToken ct)
    {
        var proof = Guid.NewGuid();
        await using var command = MigrationJournalSql.Create(owner, null, $"""
            INSERT INTO {Authorizations} (attempt_id, checkpoint_key, verification_sha256, data_role, proof_hash)
            VALUES ($1, $2, $3, $4, pg_catalog.sha256(pg_catalog.convert_to($5::uuid::text, 'UTF8')));
            """, attempt, step.Checkpoint!, step.Verify!.Sha256.ToUpperInvariant(), dataLogin, proof);
        await command.ExecuteNonQueryAsync(ct);
        return proof;
    }

    /// <summary>Reads the last checkpoint committed for this verified manifest and data step.</summary>
    internal async Task<(long Cursor, bool Finished)> ReadCheckpoint(MigrationStep step, CancellationToken ct)
    {
        await using var command = MigrationJournalSql.Create(owner, null, $"""
            SELECT b.next_cursor, b.finished
            FROM {Batches} AS b INNER JOIN {Attempts} AS a USING (attempt_id)
            WHERE a.context = $1 AND a.migration_id = $2 AND a.phase = 'data'
              AND a.step_order = $3 AND b.checkpoint_key = $4
            ORDER BY b.batch_id DESC LIMIT 1;
            """, manifest.Context, manifest.MigrationId, step.Order, step.Checkpoint!);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetInt64(0), reader.GetBoolean(1)) : (0, false);
    }

    /// <summary>Checks that this data session can still act before dispatching the SQL.</summary>
    internal async Task RequireActiveAttempt(
        NpgsqlConnection data, NpgsqlTransaction transaction, long attempt, Guid proof, long cursor, CancellationToken ct)
    {
        await using var command = MigrationJournalSql.Create(data, transaction,
            $"SELECT {CheckpointFunction}($1, $2, $3, NULL, NULL, NULL);", attempt, proof, cursor);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Confirms the owner session and records progress in the application write transaction.</summary>
    internal async Task Record(
        NpgsqlConnection data, NpgsqlTransaction transaction, long attempt, Guid proof,
        long cursor, long nextCursor, int count, bool finished, CancellationToken ct)
    {
        // Keep the batch fence held while confirming that the original, non-pooled owner is still connected.
        await using var confirmOwner = new NpgsqlCommand("SELECT 1;", owner);
        await confirmOwner.ExecuteNonQueryAsync(ct);
        await using var command = MigrationJournalSql.Create(data, transaction,
            $"SELECT {CheckpointFunction}($1, $2, $3, $4, $5, $6);", attempt, proof, cursor, nextCursor, count, finished);
        await command.ExecuteNonQueryAsync(ct);
    }
}
