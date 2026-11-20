using Npgsql;
using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Exercises the governed migration process against separate PostgreSQL principals.</summary>
[TestScope(TestScope.Integration)]
public sealed class MigrationExecutionPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Executes_ordered_expansion_and_verification_once_with_a_durable_journal()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("ordered",
        [
            new("expand", "CREATE TABLE migration_fixture.entries (position integer PRIMARY KEY); INSERT INTO migration_fixture.entries VALUES (1);"),
            new("expand", "INSERT INTO migration_fixture.entries SELECT position + 1 FROM migration_fixture.entries WHERE position = 1;"),
            new("verify", """
                SELECT array_agg(position ORDER BY position) = ARRAY[1, 2]
                       AND current_user LIKE 'mig_check_%'
                       AND NOT pg_catalog.has_schema_privilege(current_user, 'governed_migration', 'USAGE')
                FROM migration_fixture.entries;
                """)
        ], ct);

        // Act
        var result = await scenario.Run(manifest, ct);
        var retry = await scenario.Run(manifest, ct);

        // Assert
        result.ExitCode.ShouldBe(0);
        retry.ExitCode.ShouldBe(0);
        result.Output.ShouldContain("completed", StringComparison.Ordinal);
        var rows = await scenario.Read<int>(new NpgsqlCommand("SELECT count(*)::integer FROM migration_fixture.entries;"), ct);
        rows.ShouldBe(2);
        var attempts = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(phase || ':' || step_order || ':' || state, ',' ORDER BY attempt_id)
            FROM governed_migration.attempts
            WHERE context = 'catalog' AND migration_id = 'ordered' AND source_release = '1.0' AND target_release = '1.1'
              AND length(tool_version) > 0 AND length(manifest_sha256) = 64 AND length(script_sha256) = 64
              AND started_at <= finished_at;
            """), ct);
        attempts.ShouldBe("expand:1:completed,expand:2:completed,verify:3:completed");
    }

    [Fact]
    public async Task Rolls_back_a_failed_step_and_retries_only_incomplete_work_with_the_same_artifacts()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("retry",
        [
            new("expand", "CREATE TABLE migration_fixture.entries (position integer PRIMARY KEY);"),
            new("expand", """
                INSERT INTO migration_fixture.entries VALUES (1);
                DO $$ BEGIN
                    IF NOT (SELECT ready FROM migration_fixture.gate) THEN
                        RAISE EXCEPTION 'test-owned private payload';
                    END IF;
                END; $$;
                """),
            new("verify", "SELECT count(*) = 1 FROM migration_fixture.entries;")
        ], ct);

        // Act
        var failure = await scenario.Run(manifest, ct);
        failure.Error.ShouldContain("P0001", StringComparison.Ordinal);
        var rowsAfterFailure = await scenario.Read<int>(new NpgsqlCommand("SELECT count(*)::integer FROM migration_fixture.entries;"), ct);
        await scenario.ReleaseRetry(ct);
        var retry = await scenario.Run(manifest, ct);

        // Assert
        failure.ExitCode.ShouldBe(1);
        (failure.Output + failure.Error).ShouldNotContain("test-owned private payload", StringComparison.Ordinal);
        rowsAfterFailure.ShouldBe(0);
        retry.ExitCode.ShouldBe(0);
        var attempts = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(phase || ':' || step_order || ':' || state || COALESCE(':' || failure_code, ''), ',' ORDER BY attempt_id)
            FROM governed_migration.attempts;
            """), ct);
        attempts.ShouldBe("expand:1:completed,expand:2:failed:P0001,expand:2:completed,verify:3:completed");
    }

    [Theory]
    [InlineData("SELECT false;")]
    [InlineData("SELECT null::boolean;")]
    [InlineData("SELECT 'true'::text;")]
    [InlineData("SELECT 'true'::jsonb;")]
    [InlineData("SELECT true, false;")]
    [InlineData("SELECT true AS result, true AS result;")]
    [InlineData("SELECT true WHERE false;")]
    [InlineData("SELECT true UNION ALL SELECT true;")]
    [InlineData("UPDATE migration_fixture.gate SET ready = true RETURNING ready;")]
    public async Task Requires_a_single_true_boolean_from_a_read_only_verification(string sql)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("verify-gate", [new("verify", sql)], ct);

        // Act
        var failure = await scenario.Run(manifest, ct);

        // Assert
        failure.Error.ShouldContain("verify", StringComparison.Ordinal);
        failure.ExitCode.ShouldBe(1);
        var gate = await scenario.Read<bool>(new NpgsqlCommand("SELECT ready FROM migration_fixture.gate;"), ct);
        gate.ShouldBeFalse();
        var state = await scenario.Read<string>(new NpgsqlCommand("SELECT state FROM governed_migration.attempts WHERE phase = 'verify';"), ct);
        state.ShouldBe("failed");
    }

    [Fact]
    public async Task Rejects_changed_artifact_bytes_and_a_rewritten_manifest_after_completion()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("digest", [new("verify", "SELECT true;")], ct);
        var original = await scenario.Run(manifest, ct);

        // Act
        var folder = Path.GetDirectoryName(manifest).ShouldNotBeNull();
        await File.WriteAllTextAsync(Path.Combine(folder, "step-1.sql"), "SELECT 1 = 1;", ct);
        var changedArtifact = await scenario.Run(manifest, ct);
        var rewritten = await scenario.WriteManifest("digest", [new("verify", "SELECT 1 = 1;")], ct);
        var changedManifest = await scenario.Run(rewritten, ct);

        // Assert
        original.ExitCode.ShouldBe(0);
        changedArtifact.ExitCode.ShouldBe(1);
        changedArtifact.Error.ShouldContain("digest mismatch", StringComparison.Ordinal);
        changedManifest.ExitCode.ShouldBe(1);
        changedManifest.Error.ShouldContain("manifest drift", StringComparison.Ordinal);
        var count = await scenario.Read<int>(new NpgsqlCommand("SELECT count(*)::integer FROM governed_migration.attempts;"), ct);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task Refuses_sql_changed_after_execution_has_started()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("mid-run-drift",
        [
            new("expand", "SELECT pg_catalog.pg_advisory_xact_lock(813043);"),
            new("expand", "CREATE TABLE migration_fixture.entries (id integer);"),
            new("verify", "SELECT true;")
        ], ct);
        await using var blocked = await scenario.HoldGate(ct);

        // Act
        var running = scenario.Run(manifest, ct);
        _ = await scenario.AwaitBlockedExecutor(ct);
        var folder = Path.GetDirectoryName(manifest).ShouldNotBeNull();
        await File.WriteAllTextAsync(Path.Combine(folder, "step-2.sql"), "UPDATE migration_fixture.gate SET ready = true;", ct);
        await blocked.CloseAsync();
        var failure = await running;

        // Assert
        failure.ExitCode.ShouldBe(1);
        failure.Error.ShouldContain("digest mismatch", StringComparison.Ordinal);
        var gate = await scenario.Read<bool>(new NpgsqlCommand("SELECT ready FROM migration_fixture.gate;"), ct);
        gate.ShouldBeFalse();
        var count = await scenario.Read<int>(new NpgsqlCommand("SELECT count(*)::integer FROM governed_migration.attempts WHERE state = 'completed';"), ct);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task Refuses_the_same_context_while_locked_and_reconciles_an_interrupted_attempt()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("interrupted",
        [
            new("expand", "SELECT pg_catalog.pg_advisory_xact_lock(813043); CREATE TABLE migration_fixture.entries (position integer);"),
            new("verify", "SELECT to_regclass('migration_fixture.entries') IS NOT NULL;")
        ], ct);
        var another = await scenario.WriteManifest("different-migration", [new("verify", "SELECT true;")], ct);
        await using var blocked = await scenario.HoldGate(ct);

        // Act
        var first = scenario.Run(manifest, ct);
        _ = await scenario.AwaitBlockedExecutor(ct);
        var refused = await scenario.Run(another, ct);
        await scenario.InterruptBlockedExecutor(ct);
        var interrupted = await first;
        await blocked.CloseAsync();
        var retried = await scenario.Run(manifest, ct);

        // Assert
        refused.ExitCode.ShouldBe(1);
        refused.Error.ShouldContain("already locked", StringComparison.Ordinal);
        interrupted.ExitCode.ShouldBe(1);
        retried.ExitCode.ShouldBe(0);
        var attempts = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(state || COALESCE(':' || failure_code, ''), ',' ORDER BY attempt_id)
            FROM governed_migration.attempts;
            """), ct);
        attempts.ShouldBe("failed:interrupted,completed,completed");
    }

    [Theory]
    [InlineData("data", "atomic", "only atomic expansion and verification")]
    [InlineData("expand", "online", "Online execution requires a declared index")]
    [InlineData("contract", "atomic", "only atomic expansion and verification")]
    public async Task Rejects_unsupported_phases_without_starting_any_database_work(string phase, string policy, string expectedError)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        var scripts = new[] { new MigrationExecutionTestStep(phase, "SELECT 1;", policy), new MigrationExecutionTestStep("verify", "SELECT true;") };
        var manifest = await scenario.WriteManifest("unsupported", phase == "contract" ? scripts.Reverse().ToArray() : scripts, ct);

        // Act
        var refusal = await scenario.Run(manifest, ct);

        // Assert
        refusal.ExitCode.ShouldBe(1);
        refusal.Error.ShouldContain(expectedError, StringComparison.Ordinal);
        var journalExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration.attempts') IS NOT NULL;"), ct);
        journalExists.ShouldBeFalse();
    }

    [Fact]
    public async Task Refuses_an_executable_recovery_declaration_before_connecting()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        var manifest = await scenario.WriteManifest("recovery", [new("verify", "SELECT true;")], ct, recoveryKind: "reversible");

        // Act
        var refusal = await scenario.Run(manifest, ct);

        // Assert
        refusal.ExitCode.ShouldBe(1);
        refusal.Error.ShouldContain("forward-only recovery", StringComparison.Ordinal);
        var journalExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration.attempts') IS NOT NULL;"), ct);
        journalExists.ShouldBeFalse();
    }

    [Fact]
    public async Task Enforces_the_PostgreSql_lock_budget_and_rolls_back_the_step()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("lock-timeout",
        [
            new("expand", "UPDATE migration_fixture.gate SET ready = true; SELECT pg_catalog.pg_advisory_xact_lock(813043);",
                TimeoutSeconds: 10, LockTimeoutSeconds: 1),
            new("verify", "SELECT true;")
        ], ct);
        await using var blocked = await scenario.HoldGate(ct);

        // Act
        var failure = await scenario.Run(manifest, ct);

        // Assert
        failure.ExitCode.ShouldBe(1);
        failure.Error.ShouldContain("55P03", StringComparison.Ordinal);
        var gate = await scenario.Read<bool>(new NpgsqlCommand("SELECT ready FROM migration_fixture.gate;"), ct);
        gate.ShouldBeFalse();
        var state = await scenario.Read<string>(new NpgsqlCommand("SELECT state || ':' || failure_code FROM governed_migration.attempts;"), ct);
        state.ShouldBe("failed:55P03");
    }

    [Fact]
    public async Task Prevents_an_atomic_script_from_committing_outside_its_journal_transaction()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("cannot-commit",
        [
            new("expand", "CREATE TABLE migration_fixture.must_roll_back (id integer); COMMIT;"),
            new("verify", "SELECT true;")
        ], ct);

        // Act
        var failure = await scenario.Run(manifest, ct);

        // Assert
        failure.Error.ShouldContain("expand", StringComparison.Ordinal);
        failure.ExitCode.ShouldBe(1);
        var tableExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('migration_fixture.must_roll_back') IS NOT NULL;"), ct);
        tableExists.ShouldBeFalse();
        var state = await scenario.Read<string>(new NpgsqlCommand("SELECT state FROM governed_migration.attempts;"), ct);
        state.ShouldBe("failed");
    }

    [Fact]
    public async Task Requires_separate_schema_owner_and_verifier_roles()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("separate-roles", [new("verify", "SELECT true;")], ct);

        // Act
        var refusal = await scenario.Run(manifest, ct, sameRole: true);

        // Assert
        refusal.ExitCode.ShouldBe(1);
        refusal.Error.ShouldContain("separate", StringComparison.Ordinal);
        var journalExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration.attempts') IS NOT NULL;"), ct);
        journalExists.ShouldBeFalse();
    }

    [Fact]
    public async Task Rejects_a_verifier_who_can_assume_the_owner_role()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.GrantOwnerMembershipToVerifier(ct);
        var manifest = await scenario.WriteManifest("privileged-verifier", [new("verify", "SELECT true;")], ct);

        // Act
        var refusal = await scenario.Run(manifest, ct);

        // Assert
        refusal.ExitCode.ShouldBe(1);
        refusal.Error.ShouldContain("scoped logins", StringComparison.Ordinal);
        var journalExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration.attempts') IS NOT NULL;"), ct);
        journalExists.ShouldBeFalse();
    }
}
