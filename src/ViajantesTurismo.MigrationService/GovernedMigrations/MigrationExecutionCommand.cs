using System.Data;
using System.Globalization;
using System.Text.Json;
using Npgsql;

namespace ViajantesTurismo.MigrationService.GovernedMigrations;

/// <summary>Applies the supported governed migration phases before the application rolls out.</summary>
internal static class MigrationExecutionCommand
{
    /// <summary>Validates the release input and executes it with deployment-only PostgreSQL credentials.</summary>
    internal static async Task<int> Run(string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        if (args is not ["execute", var manifestPath, "--from", var sourceRelease] || string.IsNullOrWhiteSpace(manifestPath))
        {
            await error.WriteLineAsync("Usage: execute <manifest> --from <source-release>".AsMemory(), ct);
            return 2;
        }

        try
        {
            var (manifest, directory, manifestDigest) = await MigrationManifestFiles.Read(manifestPath, sourceRelease, ct);
            RequireSupportedOperations(manifest);
            foreach (var step in manifest.Steps)
            {
                _ = await MigrationArtifactFiles.ReadStepSql(directory, step, ct);
                if (step.Verify is { } verification)
                {
                    _ = await MigrationArtifactFiles.ReadVerifiedSql(directory, verification, ct);
                }
            }

            await MigrationManifestFiles.VerifyRecovery(manifest.Recovery, directory, ct);
            var (ownerSettings, verifierSettings) = MigrationPostgreSql.ConnectionSettings(
                Environment.GetEnvironmentVariable("VT_MIGRATION_OWNER_CONNECTION"),
                Environment.GetEnvironmentVariable("VT_MIGRATION_VERIFIER_CONNECTION"));
            var dataSettings = manifest.Steps.Any(static step => step.Phase == MigrationPhase.Data)
                ? MigrationPostgreSql.DataConnectionSettings(
                    Environment.GetEnvironmentVariable("VT_MIGRATION_DATA_CONNECTION"), ownerSettings, verifierSettings)
                : null;
            await using var owner = new NpgsqlConnection(ownerSettings.ConnectionString);
            await using var verifier = new NpgsqlConnection(verifierSettings.ConnectionString);
            await using var data = dataSettings is null ? null : new NpgsqlConnection(dataSettings.ConnectionString);
            await owner.OpenAsync(ct);
            await verifier.OpenAsync(ct);
            if (data is not null)
            {
                await data.OpenAsync(ct);
            }

            await MigrationPostgreSql.ValidateSessions(owner, verifier, data, ct);
            await MigrationPostgreSql.LockContext(owner, manifest.Context, ct);
            await MigrationPostgreSql.InstallScriptFunctions(owner, verifier, ct);

            var journal = new MigrationAttemptJournal(owner, manifest, manifestDigest, sourceRelease);
            await journal.Prepare(ct);

            MigrationBatchExecution? batchExecution = null;
            if (data is not null)
            {
                var batchJournal = new MigrationBatchJournal(owner, manifest);
                await batchJournal.Prepare(data.UserName!, ct);
                await MigrationBatchExecution.Install(data, ct);
                batchExecution = new MigrationBatchExecution(data, verifier, batchJournal, manifest.Context, directory);
            }

            var result = await ExecuteSteps(owner, verifier, journal, batchExecution, manifest, directory, output, error, ct);
            if (result != 0)
            {
                return result;
            }

            await output.WriteLineAsync($"Migration {manifest.Context}/{manifest.MigrationId} completed.".AsMemory(), ct);
            return 0;
        }
        catch (InvalidDataException exception)
        {
            await error.WriteLineAsync(exception.Message.AsMemory(), ct);
            return 1;
        }
        catch (JsonException)
        {
            await error.WriteLineAsync("Invalid migration manifest JSON.".AsMemory(), ct);
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            await error.WriteLineAsync("Cannot read the manifest, its artifacts, or the PostgreSQL connection settings.".AsMemory(), ct);
            return 1;
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
        {
            await error.WriteLineAsync($"Migration database setup failed ({FailureCode(exception)}).".AsMemory(), ct);
            return 1;
        }
    }

    private static void RequireSupportedOperations(MigrationManifest manifest)
    {
        if (!manifest.Steps.All(static step =>
            step is { Phase: MigrationPhase.Data, Transaction: MigrationTransaction.Batched, Destructive: false, Checkpoint: not null, Verify: not null }
                or { Phase: MigrationPhase.Expand, Transaction: MigrationTransaction.Online, Destructive: false, Checkpoint: null, Verify: not null, OnlineIndex: not null }
                or { Phase: MigrationPhase.Expand or MigrationPhase.Verify, Transaction: MigrationTransaction.Atomic, Destructive: false, Checkpoint: null, Verify: null }))
        {
            throw new InvalidDataException("The executor supports only atomic expansion and verification, online index expansion, or batched data.");
        }

        if (manifest.Steps.Any(static step => step.TimeoutSeconds > int.MaxValue / 1000))
        {
            throw new InvalidDataException("Migration timeout exceeds the PostgreSQL limit.");
        }

        if (manifest.Recovery is not { Kind: MigrationRecoveryKind.ForwardOnly, Script: null })
        {
            throw new InvalidDataException("The initial executor requires forward-only recovery evidence and no recovery script.");
        }
    }

    private static async Task<int> ExecuteSteps(
        NpgsqlConnection owner,
        NpgsqlConnection verifier,
        MigrationAttemptJournal journal,
        MigrationBatchExecution? batchExecution,
        MigrationManifest manifest,
        string directory,
        TextWriter output,
        TextWriter error,
        CancellationToken ct)
    {
        foreach (var step in manifest.Steps)
        {
            var phase = JsonNamingPolicy.CamelCase.ConvertName(step.Phase.ToString());
            var order = step.Order.ToString(CultureInfo.InvariantCulture);
            var sql = await MigrationArtifactFiles.ReadStepSql(directory, step, ct);
            if (await journal.IsComplete(step, phase, ct))
            {
                await output.WriteLineAsync($"{phase} {order} already completed.".AsMemory(), ct);
                continue;
            }

            var onlineState = MigrationOnlineIndexState.Missing;
            if (step.OnlineIndex is { } index)
            {
                onlineState = await MigrationOnlineExecution.Inspect(verifier, step, index, ct);
                var precondition = onlineState switch
                {
                    MigrationOnlineIndexState.Invalid => "online_index_invalid",
                    MigrationOnlineIndexState.Conflict => "online_index_conflict",
                    MigrationOnlineIndexState.Ready when !await journal.WasDispatched(step, phase, ct) => "online_index_exists",
                    _ => null
                };
                if (precondition is not null)
                {
                    await error.WriteLineAsync($"{phase} {order} failed ({precondition}).".AsMemory(), ct);
                    return 1;
                }
            }

            var attempt = await journal.Start(step, phase, ct);
            var failure = await ExecuteAttempt(owner, verifier, journal, batchExecution, step, sql, attempt, onlineState, directory, ct);
            if (failure is not null)
            {
                await error.WriteLineAsync($"{phase} {order} failed ({failure}).".AsMemory(), ct);
                return 1;
            }

            await output.WriteLineAsync($"{phase} {order} completed.".AsMemory(), ct);
        }

        return 0;
    }

    private static async Task<string?> ExecuteAttempt(
        NpgsqlConnection owner,
        NpgsqlConnection verifier,
        MigrationAttemptJournal journal,
        MigrationBatchExecution? batchExecution,
        MigrationStep step,
        string sql,
        long attempt,
        MigrationOnlineIndexState onlineState,
        string directory,
        CancellationToken ct)
    {
        string failure;
        try
        {
            if (step.Phase == MigrationPhase.Expand && step.Transaction == MigrationTransaction.Atomic)
            {
                await using var transaction = await owner.BeginTransactionAsync(ct);
                await MigrationPostgreSql.Expand(owner, transaction, step, sql, ct);
                await journal.Complete(attempt, transaction, ct);
                await transaction.CommitAsync(ct);
                return null;
            }

            string? result;
            if (step.OnlineIndex is { } index)
            {
                var verification = await MigrationArtifactFiles.ReadVerifiedSql(directory, step.Verify!, ct);
                result = await MigrationOnlineExecution.Run(owner, verifier, step, index, onlineState, sql, verification, ct);
            }
            else
            {
                result = step.Phase == MigrationPhase.Data
                    ? await batchExecution!.Run(step, attempt, ct)
                    : await MigrationPostgreSql.Verify(verifier, step, sql, ct) ? null : "verification_failed";
            }

            if (result is null)
            {
                await journal.Complete(attempt, null, ct);
                return null;
            }

            failure = result;
        }
        catch (Exception exception) when (exception is NpgsqlException or TimeoutException or OperationCanceledException
            or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            failure = FailureCode(exception);
        }

        if (owner.State == ConnectionState.Open)
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await journal.Fail(attempt, failure, cleanup.Token);
            }
            catch (Exception exception) when (exception is NpgsqlException or TimeoutException or OperationCanceledException)
            {
                // Keep the durable started record. A new session reconciles it after acquiring the context lock.
            }
        }

        ct.ThrowIfCancellationRequested();
        return failure;
    }

    private static string FailureCode(Exception exception) => exception switch
    {
        PostgresException postgres => postgres.SqlState,
        TimeoutException => "timeout",
        OperationCanceledException => "cancelled",
        InvalidDataException => "journal_state",
        IOException or UnauthorizedAccessException => "artifact_unavailable",
        _ => "database_error"
    };
}
