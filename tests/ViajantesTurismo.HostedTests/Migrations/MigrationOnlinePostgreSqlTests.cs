using Npgsql;
using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Exercises concurrent index creation through the governed migration process.</summary>
[TestScope(TestScope.Integration)]
public sealed class MigrationOnlinePostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Creates_a_concurrent_index_and_completes_only_after_read_only_verification()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("online-index",
        [
            new("expand", "CREATE TABLE migration_fixture.entries (position integer);"),
            new("expand", """CREATE INDEX CONCURRENTLY "entries_position" ON "migration_fixture"."entries" USING btree ("position");""",
                "online", VerifySql: """
                    SELECT current_user LIKE 'mig_check_%'
                        AND pg_catalog.current_setting('transaction_read_only') = 'on'
                        AND NOT pg_catalog.has_schema_privilege(current_user, 'migration_fixture', 'CREATE');
                    """, OnlineIndex: new { schema = "migration_fixture", table = "entries", name = "entries_position", columns = new[] { "position" }, unique = false }),
            new("verify", "SELECT true;")
        ], ct);

        // Act
        var result = await scenario.Run(manifest, ct);
        var retry = await scenario.Run(manifest, ct);

        // Assert
        result.Error.ShouldBe(string.Empty);
        result.ExitCode.ShouldBe(0);
        retry.ExitCode.ShouldBe(0);
        retry.Output.ShouldContain("expand 2 already completed", StringComparison.Ordinal);
        var valid = await scenario.Read<bool>(new NpgsqlCommand("""
            SELECT indisready AND indisvalid FROM pg_catalog.pg_index
            WHERE indexrelid = 'migration_fixture.entries_position'::regclass;
            """), ct);
        valid.ShouldBeTrue();
        var attempts = await scenario.Read<int>(new NpgsqlCommand("""
            SELECT count(*)::integer FROM governed_migration.attempts
            WHERE migration_id = 'online-index' AND phase = 'expand' AND step_order = 2 AND state = 'completed';
            """), ct);
        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task Allows_runtime_writes_and_refuses_a_second_executor_while_a_build_waits()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionOnlineData(ct);
        var manifest = await scenario.WriteManifest("online-writers", MigrationOnlineTestSteps.Index(), ct);
        var competitor = await scenario.WriteManifest("competitor", [new("verify", "SELECT true;")], ct);
        await using var control = await scenario.OpenControl(ct);
        await using var blockingTransaction = await control.BeginTransactionAsync(ct);
        await using var establishWriter = new NpgsqlCommand("INSERT INTO migration_fixture.online_entries VALUES (2, 20);", control, blockingTransaction);
        await establishWriter.ExecuteNonQueryAsync(ct);
        await using var runtime = await scenario.OpenRuntime(ct);
        await using var write = new NpgsqlCommand("INSERT INTO migration_fixture.online_entries VALUES (3, 30);", runtime) { CommandTimeout = 3 };
        await using var writeDdl = new NpgsqlCommand("CREATE TABLE migration_fixture.runtime_ddl (id integer);", runtime) { CommandTimeout = 3 };
        await using var scope = new NpgsqlCommand("""
            SELECT NOT pg_catalog.has_schema_privilege(current_user, 'migration_fixture', 'CREATE')
                AND NOT pg_catalog.has_schema_privilege(current_user, 'governed_migration', 'USAGE')
                AND NOT pg_catalog.pg_has_role((SELECT tableowner FROM pg_catalog.pg_tables
                    WHERE schemaname = 'migration_fixture' AND tablename = 'online_entries'), 'MEMBER');
            """, runtime) { CommandTimeout = 3 };

        // Act
        var active = scenario.Run(manifest, ct);
        await scenario.AwaitOnlineWait(relationLock: false, ct);
        var refusal = await scenario.Run(competitor, ct);
        var runtimeWrites = await write.ExecuteNonQueryAsync(ct);
        var scoped = await scope.ExecuteScalarAsync(ct);
        Func<Task<int>> createAsRuntime = () => writeDdl.ExecuteNonQueryAsync(ct);
        var ddlFailure = await createAsRuntime.ShouldThrow<PostgresException>();
        await blockingTransaction.CommitAsync(ct);
        var result = await active;

        // Assert
        refusal.ExitCode.ShouldBe(1);
        refusal.Error.ShouldContain("already locked", StringComparison.Ordinal);
        runtimeWrites.ShouldBe(1);
        scoped.ShouldBe(true);
        ddlFailure.SqlState.ShouldBe("42501");
        result.Error.ShouldBe(string.Empty);
        result.ExitCode.ShouldBe(0);
    }

    [Theory]
    [InlineData(false, "online_index_conflict")]
    [InlineData(true, "online_index_exists")]
    public async Task Refuses_first_attempt_if_the_index_name_is_already_owned(bool matches, string expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionOnlineData(ct);
        await using var control = await scenario.OpenControl(ct);
        await using var conflictingIndex = matches
            ? new NpgsqlCommand("CREATE INDEX online_entries_category ON migration_fixture.online_entries (category);", control)
            : new NpgsqlCommand("CREATE INDEX online_entries_category ON migration_fixture.online_entries (id);", control);
        await conflictingIndex.ExecuteNonQueryAsync(ct);
        var manifest = await scenario.WriteManifest("online-collision", MigrationOnlineTestSteps.Index(), ct);

        // Act
        var first = await scenario.Run(manifest, ct);
        var retry = await scenario.Run(manifest, ct);

        // Assert
        first.ExitCode.ShouldBe(1);
        first.Error.ShouldContain(expected, StringComparison.Ordinal);
        retry.ExitCode.ShouldBe(1);
        retry.Error.ShouldContain(expected, StringComparison.Ordinal);
        var attempts = await scenario.Read<int>(new NpgsqlCommand("""
            SELECT count(*)::integer FROM governed_migration.attempts WHERE migration_id = 'online-collision';
            """), ct);
        attempts.ShouldBe(0);
    }

    [Fact]
    public async Task Preserves_a_failed_unique_index_and_blocks_reexecution_until_recovery()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionOnlineData(ct);
        await using var control = await scenario.OpenControl(ct);
        await using var duplicate = new NpgsqlCommand("INSERT INTO migration_fixture.online_entries VALUES (2, 10);", control);
        await duplicate.ExecuteNonQueryAsync(ct);
        var manifest = await scenario.WriteManifest("online-unique", MigrationOnlineTestSteps.Index(unique: true), ct);

        // Act
        var first = await scenario.Run(manifest, ct);
        var initialIndex = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT indexrelid::text || ':' || indisvalid::text FROM pg_catalog.pg_index
            WHERE indexrelid = 'migration_fixture.online_entries_category'::regclass;
            """), ct);
        var retry = await scenario.Run(manifest, ct);
        var remainingIndex = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT indexrelid::text || ':' || indisvalid::text FROM pg_catalog.pg_index
            WHERE indexrelid = 'migration_fixture.online_entries_category'::regclass;
            """), ct);

        // Assert
        first.ExitCode.ShouldBe(1);
        first.Error.ShouldContain("23505", StringComparison.Ordinal);
        first.Error.ShouldNotContain("category", StringComparison.Ordinal);
        retry.ExitCode.ShouldBe(1);
        retry.Error.ShouldContain("online_index_invalid", StringComparison.Ordinal);
        initialIndex.ShouldEndWith(":false", StringComparison.Ordinal);
        remainingIndex.ShouldBe(initialIndex);
        var attempts = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(state || ':' || failure_code, ',' ORDER BY attempt_id) FROM governed_migration.attempts;
            """), ct);
        attempts.ShouldBe("failed:23505");
    }

    [Fact]
    public async Task Retries_after_owner_loss_before_an_index_can_be_written()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionOnlineData(ct);
        var manifest = await scenario.WriteManifest("online-before-build", MigrationOnlineTestSteps.Index(), ct);
        await using var control = await scenario.OpenControl(ct);
        await using var blocker = await control.BeginTransactionAsync(ct);
        await using var lockTable = new NpgsqlCommand("LOCK TABLE migration_fixture.online_entries IN ACCESS EXCLUSIVE MODE;", control, blocker);
        await lockTable.ExecuteNonQueryAsync(ct);

        // Act
        var active = scenario.Run(manifest, ct);
        await scenario.AwaitOnlineWait(relationLock: true, ct);
        var started = await scenario.Read<string>(new NpgsqlCommand("SELECT state FROM governed_migration.attempts;"), ct);
        await scenario.InterruptOwner(ct);
        var interrupted = await active;
        await blocker.RollbackAsync(ct);
        var existed = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('migration_fixture.online_entries_category') IS NOT NULL;"), ct);
        var retry = await scenario.Run(manifest, ct);

        // Assert
        started.ShouldBe("started");
        interrupted.ExitCode.ShouldBe(1);
        existed.ShouldBeFalse();
        retry.Error.ShouldBe(string.Empty);
        retry.ExitCode.ShouldBe(0);
        var history = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(state || COALESCE(':' || failure_code, ''), ',' ORDER BY attempt_id)
            FROM governed_migration.attempts WHERE phase = 'expand';
            """), ct);
        history.ShouldBe("failed:interrupted,completed");
    }

    [Fact]
    public async Task Reconciles_a_committed_index_after_owner_loss_during_verification()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionOnlineData(ct);
        var steps = MigrationOnlineTestSteps.Index(verification: "SELECT true FROM pg_catalog.pg_advisory_xact_lock(813043);");
        var manifest = await scenario.WriteManifest("online-after-build", steps, ct);
        await using var held = await scenario.HoldGate(ct);

        // Act
        var active = scenario.Run(manifest, ct);
        _ = await scenario.AwaitBlockedExecutor(ct);
        var committedIndex = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT indexrelid::text FROM pg_catalog.pg_index
            WHERE indisvalid AND indexrelid = 'migration_fixture.online_entries_category'::regclass;
            """), ct);
        var started = await scenario.Read<string>(new NpgsqlCommand("SELECT state FROM governed_migration.attempts;"), ct);
        await scenario.InterruptOwner(ct);
        await held.CloseAsync();
        var interrupted = await active;
        var retry = await scenario.Run(manifest, ct);

        // Assert
        started.ShouldBe("started");
        interrupted.ExitCode.ShouldBe(1);
        retry.Error.ShouldBe(string.Empty);
        retry.ExitCode.ShouldBe(0);
        var sameIndex = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT indexrelid::text FROM pg_catalog.pg_index
            WHERE indisvalid AND indexrelid = 'migration_fixture.online_entries_category'::regclass;
            """), ct);
        sameIndex.ShouldBe(committedIndex);
        var history = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(state || COALESCE(':' || failure_code, ''), ',' ORDER BY attempt_id)
            FROM governed_migration.attempts WHERE phase = 'expand';
            """), ct);
        history.ShouldBe("failed:interrupted,completed");
    }

    [Theory]
    [InlineData(20, "55P03")]
    [InlineData(1, "57014")]
    public async Task Stops_at_the_reviewed_lock_and_statement_timeouts(int timeout, string sqlState)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionOnlineData(ct);
        var manifest = await scenario.WriteManifest("online-timeout", MigrationOnlineTestSteps.Index(timeout: timeout, lockTimeout: 1), ct);
        await using var control = await scenario.OpenControl(ct);
        await using var blocker = await control.BeginTransactionAsync(ct);
        await using var lockTable = new NpgsqlCommand("LOCK TABLE migration_fixture.online_entries IN ACCESS EXCLUSIVE MODE;", control, blocker);
        await lockTable.ExecuteNonQueryAsync(ct);

        // Act
        var result = await scenario.Run(manifest, ct);
        await blocker.RollbackAsync(ct);

        // Assert
        result.ExitCode.ShouldBe(1);
        result.Error.ShouldContain(sqlState, StringComparison.Ordinal);
        var indexExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('migration_fixture.online_entries_category') IS NOT NULL;"), ct);
        indexExists.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false, "single concurrent index creation")]
    [InlineData(true, "Artifact digest mismatch")]
    public async Task Refuses_a_second_statement_or_changed_verifier_before_any_database_work(bool changeVerifier, string expectedError)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        var steps = MigrationOnlineTestSteps.Index(extraSql: changeVerifier ? string.Empty : "\nSELECT true;");
        var manifest = await scenario.WriteManifest("online-unreviewed-sql", steps, ct);
        if (changeVerifier)
        {
            var verification = Path.Combine(Path.GetDirectoryName(manifest).ShouldNotBeNull(), "verify-1.sql");
            await File.WriteAllTextAsync(verification, "SELECT false;", ct);
        }

        // Act
        var result = await scenario.Run(manifest, ct);

        // Assert
        result.ExitCode.ShouldBe(1);
        result.Error.ShouldContain(expectedError, StringComparison.Ordinal);
        var indexExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('migration_fixture.online_entries_category') IS NOT NULL;"), ct);
        indexExists.ShouldBeFalse();
        var journalExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration.attempts') IS NOT NULL;"), ct);
        journalExists.ShouldBeFalse();
    }

    [Fact]
    public async Task Rechecks_a_ready_index_without_recreating_it_after_a_false_verification_gate()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionOnlineData(ct);
        var steps = MigrationOnlineTestSteps.Index(verification: "SELECT ready FROM migration_fixture.gate;");
        var manifest = await scenario.WriteManifest("online-verification", steps, ct);

        // Act
        var first = await scenario.Run(manifest, ct);
        var committedIndex = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT indexrelid::text FROM pg_catalog.pg_index
            WHERE indisvalid AND indexrelid = 'migration_fixture.online_entries_category'::regclass;
            """), ct);
        await scenario.ReleaseRetry(ct);
        var retry = await scenario.Run(manifest, ct);

        // Assert
        first.ExitCode.ShouldBe(1);
        first.Error.ShouldContain("verification_failed", StringComparison.Ordinal);
        retry.Error.ShouldBe(string.Empty);
        retry.ExitCode.ShouldBe(0);
        var sameIndex = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT indexrelid::text FROM pg_catalog.pg_index
            WHERE indisvalid AND indexrelid = 'migration_fixture.online_entries_category'::regclass;
            """), ct);
        sameIndex.ShouldBe(committedIndex);
        var history = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(state || COALESCE(':' || failure_code, ''), ',' ORDER BY attempt_id)
            FROM governed_migration.attempts WHERE phase = 'expand';
            """), ct);
        history.ShouldBe("failed:verification_failed,completed");
    }
}
