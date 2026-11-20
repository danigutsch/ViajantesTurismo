using Npgsql;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Runs a resumed migration data step using only the named data login's application grants.</summary>
internal sealed class MigrationBatchExecution(
    NpgsqlConnection data, NpgsqlConnection verifier, MigrationBatchJournal journal, string context, string directory)
{
    /// <summary>Installs the typed, invoker-rights batch entry point on the data connection.</summary>
    internal static async Task Install(NpgsqlConnection data, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            CREATE FUNCTION pg_temp.run_governed_migration_batch(script text, cursor_value bigint, batch_limit integer)
            RETURNS TABLE (next_cursor bigint, affected_count integer, finished boolean)
            LANGUAGE plpgsql SECURITY INVOKER AS $batch$
            BEGIN
                RETURN QUERY EXECUTE script USING cursor_value, batch_limit;
            END;
            $batch$;
            """, data);
        await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Continues from the last committed cursor and requires the reviewed postcondition.</summary>
    internal async Task<string?> Run(MigrationStep step, long attempt, CancellationToken ct)
    {
        var (cursor, finished) = await journal.ReadCheckpoint(step, ct);
        if (!finished)
        {
            var proof = await journal.Authorize(attempt, step, data.UserName!, ct);
            do
            {
                var artifact = new MigrationArtifact(step.Script, step.Sha256);
                var sql = await MigrationArtifactFiles.ReadVerifiedSql(directory, artifact, ct);
                var (nextCursor, terminal, failure) = await ExecuteBatch(step, attempt, proof, cursor, sql, ct);
                if (failure is not null)
                {
                    return failure;
                }

                cursor = nextCursor;
                finished = terminal;
            } while (!finished);
        }

        var verification = await MigrationArtifactFiles.ReadVerifiedSql(directory, step.Verify!, ct);
        return await MigrationPostgreSql.Verify(verifier, step, verification, ct) ? null : "verification_failed";
    }

    private async Task<(long Cursor, bool Finished, string? Failure)> ExecuteBatch(
        MigrationStep step, long attempt, Guid proof, long cursor, string sql, CancellationToken ct)
    {
        await using var transaction = await data.BeginTransactionAsync(ct);
        await MigrationPostgreSql.LockBatch(data, transaction, context, ct);
        await MigrationPostgreSql.SetTimeouts(data, transaction, step, ct);
        await MigrationBatchJournal.RequireActiveAttempt(data, transaction, attempt, proof, cursor, ct);
        var result = await ReadResult(transaction, step, sql, cursor, ct);
        if (result is not { } batch)
        {
            return (cursor, false, "invalid_batch_result");
        }

        if (batch.Count < 0 || batch.Count > MigrationBatchJournal.BatchLimit)
        {
            return (cursor, false, "invalid_batch_result");
        }

        var progressed = batch.Count > 0 && batch.Cursor > cursor;
        var exhausted = batch.Finished && batch.Count == 0 && batch.Cursor == cursor;
        if (!progressed && !exhausted)
        {
            return (cursor, false, "batch_no_progress");
        }

        await journal.Record(data, transaction, attempt, proof, cursor, batch.Cursor, batch.Count, batch.Finished, ct);
        await transaction.CommitAsync(ct);
        return (batch.Cursor, batch.Finished, null);
    }

    private async Task<(long Cursor, int Count, bool Finished)?> ReadResult(
        NpgsqlTransaction transaction, MigrationStep step, string sql, long cursor, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(
            "SELECT * FROM pg_temp.run_governed_migration_batch($1, $2, $3);", data, transaction);
        MigrationPostgreSql.Bind(command, sql, cursor, MigrationBatchJournal.BatchLimit);
        command.CommandTimeout = step.TimeoutSeconds;
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct) || await reader.IsDBNullAsync(0, ct)
            || await reader.IsDBNullAsync(1, ct) || await reader.IsDBNullAsync(2, ct))
        {
            return null;
        }

        var result = (reader.GetInt64(0), reader.GetInt32(1), reader.GetBoolean(2));
        return await reader.ReadAsync(ct) ? null : result;
    }
}
