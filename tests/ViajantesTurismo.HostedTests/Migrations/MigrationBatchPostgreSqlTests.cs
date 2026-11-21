using Npgsql;
using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Exercises durable data checkpoints and role boundaries using the real migration process.</summary>
[TestScope(TestScope.Integration)]
public sealed class MigrationBatchPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    private const string Batch = """
        WITH picked AS (
            SELECT id FROM migration_fixture.backfill WHERE id > $1 ORDER BY id LIMIT LEAST($2, 1)
        ), mutated AS (
            UPDATE migration_fixture.backfill AS b SET updates = updates + 1
            FROM picked WHERE b.id = picked.id RETURNING b.id
        )
        SELECT COALESCE(max(id), $1), count(*)::integer, COALESCE(max(id) >= 3, false) FROM mutated;
        """;

    private const string Verify = """
        SELECT bool_and(updates = 1) AND current_user LIKE 'mig_check_%' FROM migration_fixture.backfill;
        """;

    private const string BlockingBatch = """
        WITH picked AS (
            SELECT id FROM migration_fixture.backfill WHERE id > $1 ORDER BY id LIMIT LEAST($2, 1)
        ), mutated AS (
            UPDATE migration_fixture.backfill AS b SET updates = updates + 1
            FROM picked WHERE b.id = picked.id RETURNING b.id
        ), gate AS MATERIALIZED (
            SELECT pg_catalog.pg_advisory_xact_lock(813043) FROM mutated WHERE id = 2
        )
        SELECT COALESCE(max(id), $1), count(*)::integer, COALESCE(max(id) >= 3, false)
        FROM mutated LEFT JOIN gate ON true;
        """;

    [Fact]
    public async Task Resumes_from_the_committed_cursor_after_the_data_backend_disconnects()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionBatchData(ct);
        var manifest = await scenario.WriteManifest("batch-resume",
            [new("data", BlockingBatch, "batched", VerifySql: Verify), new("verify", Verify)], ct);
        await using var hold = await scenario.HoldGate(ct);

        // Act
        var execution = scenario.Run(manifest, ct);
        _ = await scenario.AwaitBlockedExecutor(ct);
        var partial = await scenario.Read<string>(new NpgsqlCommand("SELECT string_agg(updates::text, ',' ORDER BY id) FROM migration_fixture.backfill;"), ct);
        await scenario.InterruptBlockedExecutor(ct);
        var interrupted = await execution;
        await hold.CloseAsync();
        var retry = await scenario.Run(manifest, ct);

        // Assert
        partial.ShouldBe("1,0,0");
        interrupted.ExitCode.ShouldBe(1);
        retry.ExitCode.ShouldBe(0);
        var complete = await scenario.Read<string>(new NpgsqlCommand("SELECT string_agg(updates::text, ',' ORDER BY id) FROM migration_fixture.backfill;"), ct);
        complete.ShouldBe("1,1,1");
        var checkpoints = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(source_cursor || ':' || next_cursor, ',' ORDER BY batch_id)
            FROM governed_migration.batches WHERE checkpoint_key = 'test-checkpoint';
            """), ct);
        checkpoints.ShouldBe("0:1,1:2,2:3");
    }

    [Fact]
    public async Task Fences_a_successor_until_an_interrupted_owner_data_batch_rolls_back()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionBatchData(ct);
        var manifest = await scenario.WriteManifest("batch-owner-loss",
            [new("data", BlockingBatch, "batched", VerifySql: Verify), new("verify", Verify)], ct);
        await using var hold = await scenario.HoldGate(ct);

        // Act
        var migration = scenario.Run(manifest, ct);
        _ = await scenario.AwaitBlockedExecutor(ct);
        await scenario.InterruptOwner(ct);
        var concurrent = await scenario.Run(manifest, ct);
        await hold.CloseAsync();
        var interrupted = await migration;
        var afterOwnerLoss = await scenario.Read<string>(new NpgsqlCommand("SELECT string_agg(updates::text, ',' ORDER BY id) FROM migration_fixture.backfill;"), ct);
        var retry = await scenario.Run(manifest, ct);

        // Assert
        concurrent.ExitCode.ShouldBe(1);
        concurrent.Error.ShouldContain("locked by a data batch", StringComparison.Ordinal);
        interrupted.ExitCode.ShouldBe(1);
        afterOwnerLoss.ShouldBe("1,0,0");
        retry.ExitCode.ShouldBe(0);
        var completed = await scenario.Read<string>(new NpgsqlCommand("SELECT string_agg(updates::text, ',' ORDER BY id) FROM migration_fixture.backfill;"), ct);
        completed.ShouldBe("1,1,1");
        var attempts = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(state || COALESCE(':' || failure_code, ''), ',' ORDER BY attempt_id)
            FROM governed_migration.attempts WHERE phase = 'data';
            """), ct);
        attempts.ShouldBe("failed:interrupted,completed");
    }

    [Fact]
    public async Task Rolls_back_mutation_when_a_nonterminal_batch_makes_no_progress()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionBatchData(ct);
        var manifest = await scenario.WriteManifest("no-progress",
        [
            new("data", """
                WITH changed AS (UPDATE migration_fixture.backfill SET updates = 1 WHERE id = 1 RETURNING id)
                SELECT $1::bigint, count(*)::integer, false FROM changed WHERE id <= $2;
                """, "batched"),
            new("verify", Verify)
        ], ct);

        // Act
        var failure = await scenario.Run(manifest, ct);

        // Assert
        failure.ExitCode.ShouldBe(1);
        failure.Error.ShouldContain("batch_no_progress", StringComparison.Ordinal);
        var rows = await scenario.Read<long>(new NpgsqlCommand("SELECT count(*) FROM migration_fixture.backfill WHERE updates > 0;"), ct);
        rows.ShouldBe(0L);
        var batches = await scenario.Read<long>(new NpgsqlCommand("SELECT count(*) FROM governed_migration.batches;"), ct);
        batches.ShouldBe(0L);
    }

    [Theory]
    [InlineData("SELECT $1::bigint, $2::integer + 1, true;")]
    [InlineData("SELECT $1::bigint, $2::integer, NULL::boolean;")]
    [InlineData("SELECT $1::bigint, $2::integer, true FROM pg_catalog.generate_series(1, 2);")]
    [InlineData("SELECT $1::bigint, $2::integer, true WHERE false;")]
    public async Task Rejects_an_invalid_batch_result_without_recording_a_checkpoint(string sql)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("invalid-batch-result", [new("data", sql, "batched"), new("verify", "SELECT true;")], ct);

        // Act
        var execution = await scenario.Run(manifest, ct);

        // Assert
        execution.ExitCode.ShouldBe(1);
        execution.Error.ShouldContain("invalid_batch_result", StringComparison.Ordinal);
        var checkpoints = await scenario.Read<long>(new NpgsqlCommand("SELECT count(*) FROM governed_migration.batches;"), ct);
        checkpoints.ShouldBe(0L);
    }

    [Fact]
    public async Task Accepts_the_empty_terminal_batch_and_skips_it_on_retry()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("empty-backfill",
            [new("data", "SELECT $1::bigint, (0 * $2)::integer, true;", "batched"), new("verify", "SELECT true;")], ct);

        // Act
        var execution = await scenario.Run(manifest, ct);
        var retry = await scenario.Run(manifest, ct);

        // Assert
        execution.ExitCode.ShouldBe(0);
        retry.ExitCode.ShouldBe(0);
        retry.Output.ShouldContain("data 1 already completed", StringComparison.Ordinal);
        var checkpoints = await scenario.Read<long>(new NpgsqlCommand("""
            SELECT count(*) FROM governed_migration.batches
            WHERE source_cursor = 0 AND next_cursor = 0 AND affected_count = 0 AND finished;
            """), ct);
        checkpoints.ShouldBe(1L);
    }

    [Fact]
    public async Task Retains_terminal_batches_when_the_separate_postcondition_fails()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionBatchData(ct);
        var manifest = await scenario.WriteManifest("postcondition",
        [
            new("data", Batch, "batched", VerifySql: "SELECT ready FROM migration_fixture.gate;"),
            new("verify", Verify)
        ], ct);

        // Act
        var first = await scenario.Run(manifest, ct);
        await scenario.ReleaseRetry(ct);
        var retry = await scenario.Run(manifest, ct);

        // Assert
        first.ExitCode.ShouldBe(1);
        first.Error.ShouldContain("verification_failed", StringComparison.Ordinal);
        retry.ExitCode.ShouldBe(0);
        var counts = await scenario.Read<string>(new NpgsqlCommand("SELECT string_agg(updates::text, ',' ORDER BY id) FROM migration_fixture.backfill;"), ct);
        counts.ShouldBe("1,1,1");
        var states = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(state || COALESCE(':' || failure_code, ''), ',' ORDER BY attempt_id)
            FROM governed_migration.attempts WHERE phase = 'data';
            """), ct);
        states.ShouldBe("failed:verification_failed,completed");
    }

    [Fact]
    public async Task Refuses_drift_after_a_data_checkpoint_has_been_committed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionBatchData(ct);
        var original = await scenario.WriteManifest("batch-drift", [new("data", Batch, "batched"), new("verify", Verify)], ct);
        var execution = await scenario.Run(original, ct);

        // Act
        var edited = await scenario.WriteManifest("batch-drift", [new("data", Batch + " ", "batched"), new("verify", Verify)], ct);
        var failure = await scenario.Run(edited, ct);

        // Assert
        execution.ExitCode.ShouldBe(0);
        failure.ExitCode.ShouldBe(1);
        failure.Error.ShouldContain("manifest drift", StringComparison.Ordinal);
        var checkpoints = await scenario.Read<long>(new NpgsqlCommand("SELECT count(*) FROM governed_migration.batches;"), ct);
        checkpoints.ShouldBe(3L);
    }

    [Theory]
    [InlineData("WITH changed AS (DELETE FROM governed_migration.attempts RETURNING attempt_id) SELECT $1::bigint, count(*)::integer, true FROM changed WHERE $2 > 0;")]
    [InlineData("WITH changed AS (INSERT INTO governed_migration.batches (source_cursor) VALUES (10) RETURNING batch_id) SELECT $1::bigint, count(*)::integer, true FROM changed WHERE $2 > 0;")]
    public async Task Keeps_data_migration_separate_from_schema_ownership_and_direct_journal_writes(string sql)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionBatchData(ct);
        var manifest = await scenario.WriteManifest("data-privilege", [new("data", sql, "batched"), new("verify", Verify)], ct);

        // Act
        var failure = await scenario.Run(manifest, ct);
        var sameRole = await scenario.Run(manifest, ct, dataUsesOwner: true);
        var canCreateObjects = await scenario.Read<bool>(
            new NpgsqlCommand("SELECT pg_catalog.has_schema_privilege(current_user, 'migration_fixture', 'CREATE');"), ct, asDataMigrator: true);

        // Assert
        failure.ExitCode.ShouldBe(1);
        failure.Error.ShouldContain("42501", StringComparison.Ordinal);
        sameRole.ExitCode.ShouldBe(1);
        sameRole.Error.ShouldContain("separate", StringComparison.Ordinal);
        canCreateObjects.ShouldBeFalse();
        var attempts = await scenario.Read<long>(new NpgsqlCommand("SELECT count(*) FROM governed_migration.attempts;"), ct);
        attempts.ShouldBe(1L);
    }
}
