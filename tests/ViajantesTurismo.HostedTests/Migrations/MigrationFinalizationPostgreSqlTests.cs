using Npgsql;
using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Exercises release retirement and declared recovery with scoped PostgreSQL principals.</summary>
[TestScope(TestScope.Integration)]
public sealed class MigrationFinalizationPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Keeps_legacy_schema_during_preparation_and_contracts_only_with_a_retirement_receipt()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("release-contract",
        [
            new("expand", "CREATE TABLE migration_fixture.entries (id integer PRIMARY KEY, legacy integer);"),
            new("verify", "SELECT count(*) = 0 FROM migration_fixture.entries;"),
            new("contract", "ALTER TABLE migration_fixture.entries DROP COLUMN legacy;")
        ], ct);
        var receipt = await scenario.WriteRetirement(manifest, "release-contract", ct);

        // Act
        var prepare = await scenario.Run(manifest, ct);
        prepare.Error.ShouldBe(string.Empty);
        prepare.ExitCode.ShouldBe(0);
        var oldSchema = await scenario.Read<bool>(new NpgsqlCommand("""
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_attribute
                WHERE attrelid = 'migration_fixture.entries'::regclass AND attname = 'legacy' AND NOT attisdropped);
            """), ct);
        await using var control = await scenario.OpenControl(ct);
        await using var legacyJournal = new NpgsqlCommand("""
            ALTER TABLE governed_migration.attempts DROP CONSTRAINT migration_attempt_phase_v2;
            ALTER TABLE governed_migration.attempts ADD CONSTRAINT migration_attempt_phase CHECK (phase IN ('expand', 'data', 'verify'));
            ALTER TABLE governed_migration.attempts DROP COLUMN retirement_sha256;
            """, control);
        await legacyJournal.ExecuteNonQueryAsync(ct);
        var missing = await scenario.Run(manifest, ct, action: "contract");
        var finalized = await scenario.Run(manifest, ct, action: "contract", retirement: receipt);
        var retry = await scenario.Run(manifest, ct, action: "contract", retirement: receipt);
        var prepareAgain = await scenario.Run(manifest, ct);

        // Assert
        oldSchema.ShouldBeTrue();
        prepare.Output.ShouldContain("Contract requires retirement evidence", StringComparison.Ordinal);
        prepareAgain.ExitCode.ShouldBe(0);
        prepareAgain.Output.ShouldNotContain("Contract requires retirement evidence", StringComparison.Ordinal);
        missing.ExitCode.ShouldBe(2);
        finalized.Error.ShouldBe(string.Empty);
        finalized.ExitCode.ShouldBe(0);
        retry.Output.ShouldContain("contract 3 already completed", StringComparison.Ordinal);
        var removed = await scenario.Read<bool>(new NpgsqlCommand("""
            SELECT NOT EXISTS (SELECT 1 FROM pg_catalog.pg_attribute
                WHERE attrelid = 'migration_fixture.entries'::regclass AND attname = 'legacy' AND NOT attisdropped);
            """), ct);
        removed.ShouldBeTrue();
        var history = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(phase || ':' || state, ',' ORDER BY attempt_id) FROM governed_migration.attempts;
            """), ct);
        history.ShouldBe("expand:completed,verify:completed,contract:completed");
        var recordedReceipt = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT retirement_sha256 FROM governed_migration.attempts WHERE phase = 'contract';
            """), ct);
        recordedReceipt.ShouldBe(receipt.Sha256);
    }

    [Theory]
    [InlineData("reversible")]
    [InlineData("compensating")]
    public async Task Applies_declared_recovery_once_and_rechecks_the_read_only_gate_on_retry(string recoveryKind)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("declared-recovery",
        [
            new("expand", "CREATE TABLE migration_fixture.entries (id integer PRIMARY KEY, revision integer NOT NULL); INSERT INTO migration_fixture.entries VALUES (1, 0);"),
            new("verify", "SELECT count(*) = 1 FROM migration_fixture.entries;")
        ], ct, recoveryKind: recoveryKind, recoverySql: "UPDATE migration_fixture.entries SET revision = revision + 1;",
        recoveryVerification: """
            SELECT ready AND current_user LIKE 'mig_check_%' AND pg_catalog.current_setting('transaction_read_only') = 'on'
            FROM migration_fixture.gate;
            """);

        // Act
        var prepare = await scenario.Run(manifest, ct);
        prepare.Error.ShouldBe(string.Empty);
        prepare.ExitCode.ShouldBe(0);
        var wrongRole = await scenario.Run(manifest, ct, sameRole: true, action: "recover");
        var first = await scenario.Run(manifest, ct, action: "recover");
        await scenario.ReleaseRetry(ct);
        var retry = await scenario.Run(manifest, ct, action: "recover");
        var forwardAfterRecovery = await scenario.Run(manifest, ct);

        // Assert
        wrongRole.ExitCode.ShouldBe(1);
        wrongRole.Error.ShouldContain("separate PostgreSQL logins", StringComparison.Ordinal);
        first.ExitCode.ShouldBe(1);
        first.Error.ShouldContain("verification_failed", StringComparison.Ordinal);
        retry.Error.ShouldBe(string.Empty);
        retry.ExitCode.ShouldBe(0);
        forwardAfterRecovery.ExitCode.ShouldBe(1);
        forwardAfterRecovery.Error.ShouldContain("recovery has started", StringComparison.Ordinal);
        var appliedOnce = await scenario.Read<int>(new NpgsqlCommand("SELECT revision FROM migration_fixture.entries WHERE id = 1;"), ct);
        appliedOnce.ShouldBe(1);
        var history = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(step_order || ':' || state || COALESCE(':' || failure_code, ''), ',' ORDER BY attempt_id)
            FROM governed_migration.attempts WHERE phase = 'recover';
            """), ct);
        history.ShouldBe("1:completed,2:failed:verification_failed,2:completed");
    }

    [Theory]
    [InlineData("predecessor", "Retirement evidence does not match")]
    [InlineData("target", "Retirement evidence does not match")]
    [InlineData("rehearsal", "Retirement evidence does not match")]
    [InlineData("digest", "Artifact digest mismatch")]
    [InlineData("oversize", "Migration artifact exceeds the size limit")]
    public async Task Refuses_unbound_retirement_evidence_before_any_database_writes(string change, string expectedError)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        var manifest = await scenario.WriteManifest("unbound-retirement",
        [
            new("verify", "SELECT true;"),
            new("contract", "CREATE TABLE migration_fixture.unbound (id integer);")
        ], ct, extraSource: change == "predecessor" ? "0.9" : string.Empty);
        var receipt = await scenario.WriteRetirement(manifest, "unbound-retirement", ct,
            deploymentTarget: change == "target" ? "different-target" : "synthetic-test", rehearsalPassed: change != "rehearsal");
        if (change == "digest")
        {
            await File.AppendAllTextAsync(receipt.Path, " ", ct);
        }
        else if (change == "oversize")
        {
            await File.WriteAllTextAsync(receipt.Path, new string(' ', 64 * 1024 + 1), ct);
        }

        // Act
        var result = await scenario.Run(manifest, ct, action: "contract", retirement: receipt);

        // Assert
        result.ExitCode.ShouldBe(1);
        result.Error.ShouldContain(expectedError, StringComparison.Ordinal);
        var journalExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration.attempts') IS NOT NULL;"), ct);
        journalExists.ShouldBeFalse();
    }

    [Fact]
    public async Task Requires_completed_compatibility_verification_even_after_retirement()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("contract-compatibility",
        [
            new("expand", "CREATE TABLE migration_fixture.legacy_entries (id integer PRIMARY KEY);"),
            new("verify", "SELECT ready FROM migration_fixture.gate;"),
            new("contract", "DROP TABLE migration_fixture.legacy_entries;")
        ], ct);
        var receipt = await scenario.WriteRetirement(manifest, "contract-compatibility", ct);

        // Act
        var unverified = await scenario.Run(manifest, ct);
        var rejected = await scenario.Run(manifest, ct, action: "contract", retirement: receipt);
        var preserved = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('migration_fixture.legacy_entries') IS NOT NULL;"), ct);
        await scenario.ReleaseRetry(ct);
        var ready = await scenario.Run(manifest, ct);
        var applied = await scenario.Run(manifest, ct, action: "contract", retirement: receipt);

        // Assert
        unverified.Error.ShouldContain("verification_failed", StringComparison.Ordinal);
        rejected.ExitCode.ShouldBe(1);
        rejected.Error.ShouldContain("requires completed compatibility preparation and verification", StringComparison.Ordinal);
        preserved.ShouldBeTrue();
        ready.ExitCode.ShouldBe(0);
        applied.ExitCode.ShouldBe(0);
        var removed = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('migration_fixture.legacy_entries') IS NULL;"), ct);
        removed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("forwardOnly")]
    [InlineData("restore")]
    public async Task Refuses_to_execute_recovery_that_belongs_to_an_external_procedure(string kind)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        var manifest = await scenario.WriteManifest("external-recovery", [new("verify", "SELECT true;")], ct, recoveryKind: kind);

        // Act
        var result = await scenario.Run(manifest, ct, action: "recover");

        // Assert
        result.ExitCode.ShouldBe(1);
        result.Error.ShouldContain("performed externally", StringComparison.Ordinal);
        var journalExists = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration.attempts') IS NOT NULL;"), ct);
        journalExists.ShouldBeFalse();
    }

    [Fact]
    public async Task Refuses_recovery_before_the_manifest_has_a_mutation_history()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("recovery-without-history",
        [
            new("expand", "CREATE TABLE migration_fixture.entries (id integer PRIMARY KEY);"),
            new("verify", "SELECT true;")
        ], ct, recoveryKind: "reversible", recoverySql: "CREATE TABLE migration_fixture.unapproved (id integer);",
        recoveryVerification: "SELECT true;");

        // Act
        var result = await scenario.Run(manifest, ct, action: "recover");

        // Assert
        result.ExitCode.ShouldBe(1);
        result.Error.ShouldContain("requires a previous mutation attempt", StringComparison.Ordinal);
        var tables = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('migration_fixture.unapproved') IS NULL;"), ct);
        tables.ShouldBeTrue();
    }

    [Fact]
    public async Task Rolls_back_recovery_when_the_owner_disconnects_before_the_atomic_commit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        var manifest = await scenario.WriteManifest("interrupted-recovery",
        [
            new("expand", "CREATE TABLE migration_fixture.entries (revision integer NOT NULL); INSERT INTO migration_fixture.entries VALUES (0);"),
            new("verify", "SELECT true;")
        ], ct, recoveryKind: "reversible", recoverySql: """
            UPDATE migration_fixture.entries SET revision = revision + 1;
            SELECT pg_catalog.pg_advisory_xact_lock(813043);
            """, recoveryVerification: "SELECT revision = 1 FROM migration_fixture.entries;");
        var prepared = await scenario.Run(manifest, ct);
        prepared.ExitCode.ShouldBe(0);
        await using var gate = await scenario.HoldGate(ct);

        // Act
        var active = scenario.Run(manifest, ct, action: "recover");
        _ = await scenario.AwaitBlockedExecutor(ct);
        var concurrent = await scenario.Run(manifest, ct);
        await scenario.InterruptOwner(ct);
        var interrupted = await active;
        var beforeRetry = await scenario.Read<int>(new NpgsqlCommand("SELECT revision FROM migration_fixture.entries;"), ct);
        await gate.CloseAsync();
        var retry = await scenario.Run(manifest, ct, action: "recover");

        // Assert
        concurrent.Error.ShouldContain("already locked", StringComparison.Ordinal);
        interrupted.ExitCode.ShouldBe(1);
        beforeRetry.ShouldBe(0);
        retry.Error.ShouldBe(string.Empty);
        retry.ExitCode.ShouldBe(0);
        var value = await scenario.Read<int>(new NpgsqlCommand("SELECT revision FROM migration_fixture.entries;"), ct);
        value.ShouldBe(1);
        var history = await scenario.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(step_order || ':' || state || COALESCE(':' || failure_code, ''), ',' ORDER BY attempt_id)
            FROM governed_migration.attempts WHERE phase = 'recover';
            """), ct);
        history.ShouldBe("1:failed:interrupted,1:completed,2:completed");
    }
}
