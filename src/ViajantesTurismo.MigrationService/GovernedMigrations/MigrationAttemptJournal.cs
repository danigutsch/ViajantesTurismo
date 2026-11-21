using System.Reflection;
using Npgsql;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Records migration attempts on the PostgreSQL session holding the context lock.</summary>
internal sealed class MigrationAttemptJournal(
    NpgsqlConnection owner, MigrationManifest manifest, MigrationJournalSchema journalSchema, string manifestDigest, string sourceRelease)
{
    private string Attempts => journalSchema.Attempts;
    private static readonly string ToolVersion = typeof(MigrationAttemptJournal).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    /// <summary>Initializes journal storage, checks for drift, and closes attempts whose session ended.</summary>
    internal async Task Prepare(CancellationToken ct)
    {
        await using var transaction = await owner.BeginTransactionAsync(ct);
        await MigrationPostgreSql.LockBatch(owner, transaction, manifest.Context, ct);
        await journalSchema.ValidateProvisioning(owner, transaction, manifest.Context, ct);
        await EnsureStorage(transaction, ct);
        await using var check = MigrationJournalSql.Create(owner, transaction, $"""
            SELECT EXISTS (
                SELECT 1 FROM {Attempts}
                WHERE context = $1 AND migration_id = $2
                  AND (manifest_sha256 <> $3 OR source_release <> $4 OR target_release <> $5)
            );
            """, manifest.Context, manifest.MigrationId, manifestDigest, sourceRelease, manifest.TargetRelease);
        if (await check.ExecuteScalarAsync(ct) is true)
        {
            throw new InvalidDataException("Migration manifest drift or source release mismatch in the attempt journal.");
        }

        await using var reconcile = MigrationJournalSql.Create(owner, transaction, $"""
            UPDATE {Attempts}
            SET state = 'failed', finished_at = pg_catalog.clock_timestamp(), failure_code = 'interrupted'
            WHERE context = $1 AND state = 'started';
            """, manifest.Context);
        await reconcile.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }

    /// <summary>Returns whether the step completed with this exact script digest.</summary>
    internal async Task<bool> IsComplete(MigrationStep step, string phase, CancellationToken ct)
    {
        await using var command = MigrationJournalSql.Create(owner, null, $"""
            SELECT script_sha256 FROM {Attempts}
            WHERE context = $1 AND migration_id = $2 AND phase = $3 AND step_order = $4 AND state = 'completed';
            """, manifest.Context, manifest.MigrationId, phase, step.Order);
        var digest = await command.ExecuteScalarAsync(ct) as string;
        if (digest is not null && !string.Equals(digest, step.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Migration script drift in the attempt journal.");
        }

        return digest is not null;
    }

    /// <summary>Returns whether this exact step previously reached durable dispatch.</summary>
    internal async Task<bool> WasDispatched(MigrationStep step, string phase, CancellationToken ct)
    {
        await using var command = MigrationJournalSql.Create(owner, null, $"""
            SELECT EXISTS (
                SELECT 1 FROM {Attempts}
                WHERE context = $1 AND migration_id = $2 AND phase = $3 AND step_order = $4 AND script_sha256 = $5
            );
            """, manifest.Context, manifest.MigrationId, phase, step.Order, step.Sha256.ToUpperInvariant());
        return await command.ExecuteScalarAsync(ct) is true;
    }

    /// <summary>Commits a new attempt before dispatching the step.</summary>
    internal async Task<long> Start(MigrationStep step, string phase, string? retirementDigest, CancellationToken ct)
    {
        await using var command = MigrationJournalSql.Create(owner, null, $"""
            INSERT INTO {Attempts}
                (context, migration_id, phase, step_order, manifest_sha256, script_sha256, source_release, target_release, tool_version, retirement_sha256)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, NULLIF($10, ''))
            RETURNING attempt_id;
            """, manifest.Context, manifest.MigrationId, phase, step.Order,
            manifestDigest, step.Sha256.ToUpperInvariant(), sourceRelease, manifest.TargetRelease, ToolVersion, retirementDigest ?? string.Empty);
        return (long)(await command.ExecuteScalarAsync(ct))!;
    }

    /// <summary>Prevents forward dispatch after recovery started, and recovery without prior mutation work.</summary>
    internal async Task RequireLifecycle(bool recovery, CancellationToken ct)
    {
        await using var command = MigrationJournalSql.Create(owner, null, $"""
            SELECT EXISTS (
                SELECT 1 FROM {Attempts} WHERE context = $1 AND migration_id = $2
                    AND (($3 AND phase IN ('expand', 'data', 'contract')) OR (NOT $3 AND phase = 'recover'))
            );
            """, manifest.Context, manifest.MigrationId, recovery);
        if ((await command.ExecuteScalarAsync(ct) is true) != recovery)
        {
            throw new InvalidDataException(recovery
                ? "Recovery requires a previous mutation attempt for this migration."
                : "Migration recovery has started. Use a new reviewed forward migration.");
        }
    }

    /// <summary>Records completion, optionally within the transaction containing the expansion SQL.</summary>
    internal async Task Complete(long attempt, NpgsqlTransaction? transaction, CancellationToken ct)
    {
        await using var command = MigrationJournalSql.Create(owner, transaction, $"""
            UPDATE {Attempts} SET state = 'completed', finished_at = pg_catalog.clock_timestamp()
            WHERE attempt_id = $1 AND state = 'started';
            """, attempt);
        if (await command.ExecuteNonQueryAsync(ct) != 1)
        {
            throw new InvalidDataException("Migration attempt is no longer open.");
        }
    }

    /// <summary>Records a sanitized failure if the attempt is still open.</summary>
    internal async Task Fail(long attempt, string code, CancellationToken ct)
    {
        await using var command = MigrationJournalSql.Create(owner, null, $"""
            UPDATE {Attempts}
            SET state = 'failed', finished_at = pg_catalog.clock_timestamp(), failure_code = $2
            WHERE attempt_id = $1 AND state = 'started';
            """, attempt, code);
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task EnsureStorage(NpgsqlTransaction transaction, CancellationToken ct)
    {
        // Two-integer advisory keys use a different PostgreSQL namespace than the context keys.
        await using var lockStorage = new NpgsqlCommand(
            "SELECT pg_catalog.pg_advisory_xact_lock(20261118, 46);", owner, transaction);
        await lockStorage.ExecuteNonQueryAsync(ct);
        await using var create = new NpgsqlBatch(owner, transaction)
        {
            BatchCommands =
            {
                new NpgsqlBatchCommand($"""
                    CREATE TABLE IF NOT EXISTS {Attempts} (
                        attempt_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        context text NOT NULL,
                        migration_id text NOT NULL,
                        phase text NOT NULL CONSTRAINT migration_attempt_phase_v2 CHECK (phase IN ('expand', 'data', 'verify', 'contract', 'recover')),
                        step_order integer NOT NULL CHECK (step_order > 0),
                        manifest_sha256 text NOT NULL CHECK (length(manifest_sha256) = 64),
                        script_sha256 text NOT NULL CHECK (length(script_sha256) = 64),
                        retirement_sha256 text CHECK (length(retirement_sha256) = 64),
                        source_release text NOT NULL,
                        target_release text NOT NULL,
                        tool_version text NOT NULL,
                        state text NOT NULL DEFAULT 'started',
                        started_at timestamptz NOT NULL DEFAULT pg_catalog.clock_timestamp(),
                        finished_at timestamptz,
                        failure_code text,
                        CONSTRAINT migration_attempt_retirement CHECK ((phase = 'contract') = (retirement_sha256 IS NOT NULL)),
                        CHECK (
                            (state = 'started' AND finished_at IS NULL AND failure_code IS NULL)
                            OR (state = 'completed' AND finished_at IS NOT NULL AND failure_code IS NULL)
                            OR (state = 'failed' AND finished_at IS NOT NULL AND failure_code IS NOT NULL)
                        )
                    );
                    """),
                new NpgsqlBatchCommand($"""
                    DO $journal$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT FROM pg_catalog.pg_constraint
                            WHERE conrelid = '{Attempts}'::pg_catalog.regclass AND conname = 'migration_attempt_phase_v2'
                        ) THEN
                            ALTER TABLE {Attempts} DROP CONSTRAINT migration_attempt_phase;
                            ALTER TABLE {Attempts} ADD CONSTRAINT migration_attempt_phase_v2
                                CHECK (phase IN ('expand', 'data', 'verify', 'contract', 'recover'));
                            ALTER TABLE {Attempts} ADD COLUMN retirement_sha256 text CHECK (length(retirement_sha256) = 64);
                            ALTER TABLE {Attempts} ADD CONSTRAINT migration_attempt_retirement
                                CHECK ((phase = 'contract') = (retirement_sha256 IS NOT NULL));
                        END IF;
                    END;
                    $journal$;
                    """),
                new NpgsqlBatchCommand($"""
                    CREATE UNIQUE INDEX IF NOT EXISTS completed_migration_steps
                        ON {Attempts} (context, migration_id, phase, step_order) WHERE state = 'completed';
                    """),
            }
        };
        await create.ExecuteNonQueryAsync(ct);
    }
}
