using System.Diagnostics;
using Npgsql;
using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Rehearses reviewed SQL against application-readable synthetic Catalog data.</summary>
[TestScope(TestScope.Integration)]
public sealed class CatalogMigrationRehearsalPostgreSqlTests(PostgreSqlTestServerFixture fixture, ITestOutputHelper output)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task Resumes_and_reverses_the_upgrade_with_the_original_catalog_readable()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scope = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scope.Provision(ct);
        await scope.RestoreCatalogSnapshot(ct);
        await using var reader = new CatalogMigrationRehearsalReader(scope.RuntimeConnectionString);
        var original = await reader.ReadPortuguese(ct);
        var manifest = await scope.WriteManifest("catalog-rehearsal", CatalogMigrationRehearsalArtifacts.Upgrade, ct,
            recoveryKind: "reversible", recoverySql: CatalogMigrationRehearsalArtifacts.RestorePreviousSchema,
            recoveryVerification: CatalogMigrationRehearsalArtifacts.VerifyRecovery);
        await using var runtime = await scope.OpenRuntime(ct);
        await using var ddl = new NpgsqlCommand("CREATE TABLE public.rehearsal_runtime_must_not_create (id integer);", runtime);
        await using var temporaryDdl = new NpgsqlCommand("CREATE TEMP TABLE rehearsal_runtime_temp (id integer);", runtime);
        Func<Task> createAsRuntime = () => ddl.ExecuteNonQueryAsync(ct);
        Func<Task> createTemporaryAsRuntime = () => temporaryDdl.ExecuteNonQueryAsync(ct);
        var elapsed = Stopwatch.StartNew();

        // Act
        var refusedDdl = await createAsRuntime.ShouldThrow<PostgresException>();
        var refusedTemporaryDdl = await createTemporaryAsRuntime.ShouldThrow<PostgresException>();
        var paused = await scope.Run(manifest, ct);
        var afterPause = await reader.ReadPortuguese(ct);
        await scope.ReleaseRetry(ct);
        var resumed = await scope.Run(manifest, ct);
        var afterUpgrade = await reader.ReadPortuguese(ct);
        var backfilled = await scope.Read<int>(new NpgsqlCommand("""
            SELECT "RehearsalRevision" FROM public."PublicContent" WHERE "Key" = 'HOME.REHEARSAL';
            """), ct);
        var recovery = await scope.Run(manifest, ct, action: "recover");
        var restored = await reader.ReadPortuguese(ct);
        var storedHistory = await scope.Read<string>(new NpgsqlCommand("""
            SELECT "MigrationId" FROM public."__EFMigrationsHistory";
            """), ct);
        var manifestDigest = await scope.Read<string>(new NpgsqlCommand("""
            SELECT max(manifest_sha256) FROM governed_migration.attempts WHERE migration_id = 'catalog-rehearsal';
            """), ct);

        // Assert
        original.ShouldBe(("pt-BR", "Página de ensaio", "Conteúdo sintético"));
        refusedDdl.SqlState.ShouldBe("42501");
        refusedTemporaryDdl.SqlState.ShouldBe("42501");
        paused.ExitCode.ShouldBe(1);
        paused.Error.ShouldContain("data 2 failed (verification_failed)", StringComparison.Ordinal);
        afterPause.ShouldBe(original);
        resumed.Error.ShouldBe(string.Empty);
        resumed.ExitCode.ShouldBe(0);
        afterUpgrade.ShouldBe(original);
        backfilled.ShouldBe(1);
        recovery.Error.ShouldBe(string.Empty);
        recovery.ExitCode.ShouldBe(0);
        restored.ShouldBe(original);
        storedHistory.ShouldBe("20261111091535_InitialCatalog");
        manifestDigest.Length.ShouldBe(64);
        output.WriteLine($"Catalog rehearsal 1.0 -> 1.1, snapshot SHA-256 {MigrationExecutionPostgreSqlScenario.CatalogSnapshotDigest}, "
            + $"manifest SHA-256 {manifestDigest}, elapsed {elapsed.ElapsedMilliseconds} ms, stored variants 2.");
    }

    [Fact]
    public async Task Rejects_recovery_that_removes_a_published_catalog_variant()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scope = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scope.Provision(ct);
        await scope.RestoreCatalogSnapshot(ct);
        await using var reader = new CatalogMigrationRehearsalReader(scope.RuntimeConnectionString);
        var before = await reader.ReadPortuguese(ct);
        var manifest = await scope.WriteManifest("lossy-catalog-recovery", CatalogMigrationRehearsalArtifacts.Upgrade,
            ct, recoveryKind: "reversible", recoverySql: CatalogMigrationRehearsalArtifacts.DropPortugueseVariant,
            recoveryVerification: CatalogMigrationRehearsalArtifacts.VerifyRecovery);
        await scope.ReleaseRetry(ct);
        var forward = await scope.Run(manifest, ct);
        forward.Error.ShouldBe(string.Empty);
        forward.ExitCode.ShouldBe(0);

        // Act
        var recovery = await scope.Run(manifest, ct, action: "recover");
        var restored = await reader.ReadPortuguese(ct);

        // Assert
        before.Language.ShouldBe("pt-BR");
        restored.Language.ShouldBe("en-US");
        recovery.ExitCode.ShouldBe(1);
        recovery.Error.ShouldContain("verification_failed", StringComparison.Ordinal);
    }
}
