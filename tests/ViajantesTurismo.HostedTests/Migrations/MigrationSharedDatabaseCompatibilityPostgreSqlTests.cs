using Npgsql;
using PandaBear.Testing;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Exercises independent migration journals for two context owners on one PostgreSQL database.</summary>
[TestScope(TestScope.Integration)]
public sealed class MigrationSharedDatabaseCompatibilityPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Completes_and_resumes_both_contexts_with_separate_owners_and_checkpoints(bool catalogUsesDefault)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionBatchData(ct);
        await scenario.ProvisionSharedBranding(ct);
        var catalog = await scenario.WriteManifest("shared-work",
        [
            new("expand", "CREATE TABLE migration_fixture.catalog_record (id integer PRIMARY KEY);"),
            new("data", """
                WITH selected AS (SELECT id FROM migration_fixture.backfill WHERE id > $1 ORDER BY id LIMIT $2),
                changed AS (UPDATE migration_fixture.backfill AS row SET updates = updates + 1
                    FROM selected WHERE row.id = selected.id RETURNING row.id)
                SELECT COALESCE(max(id), $1), count(*)::integer, count(*) = 0 FROM changed;
                """, "batched", VerifySql: "SELECT sum(updates) = 3 FROM migration_fixture.backfill;"),
            new("verify", "SELECT count(*) = 0 FROM migration_fixture.catalog_record;")
        ], ct, journalSchema: catalogUsesDefault ? string.Empty : "governed_migration_catalog");
        var branding = await scenario.WriteManifest("shared-work-branding",
        [
            new("expand", "CREATE TABLE branding_fixture.branding_record (id integer PRIMARY KEY);"),
            new("data", """
                WITH selected AS (SELECT id FROM branding_fixture.backfill WHERE id > $1 ORDER BY id LIMIT $2),
                changed AS (UPDATE branding_fixture.backfill AS row SET updates = updates + 1
                    FROM selected WHERE row.id = selected.id RETURNING row.id)
                SELECT COALESCE(max(id), $1), count(*)::integer, count(*) = 0 FROM changed;
                """, "batched", VerifySql: "SELECT sum(updates) = 3 FROM branding_fixture.backfill;"),
            new("verify", "SELECT count(*) = 0 FROM branding_fixture.branding_record;")
        ], ct, context: "branding", journalSchema: "governed_migration_branding");

        // Act
        var catalogResult = await scenario.Run(catalog, ct);
        var brandingResult = await scenario.Run(branding, ct, asBranding: true);
        var catalogRetry = await scenario.Run(catalog, ct);
        var brandingRetry = await scenario.Run(branding, ct, asBranding: true);

        // Assert
        catalogResult.Error.ShouldBe(string.Empty);
        catalogResult.ExitCode.ShouldBe(0);
        brandingResult.Error.ShouldBe(string.Empty);
        brandingResult.ExitCode.ShouldBe(0);
        catalogRetry.Output.ShouldContain("data 2 already completed", StringComparison.Ordinal);
        brandingRetry.Output.ShouldContain("data 2 already completed", StringComparison.Ordinal);
        var catalogAttempts = await scenario.Read<long>(catalogUsesDefault
            ? new NpgsqlCommand("SELECT count(*) FROM governed_migration.attempts WHERE context = 'catalog' AND state = 'completed';")
            : new NpgsqlCommand("SELECT count(*) FROM governed_migration_catalog.attempts WHERE context = 'catalog' AND state = 'completed';"), ct);
        catalogAttempts.ShouldBe(3);
        var brandingBatches = await scenario.Read<long>(new NpgsqlCommand("""
            SELECT count(*) FROM governed_migration_branding.batches
            JOIN governed_migration_branding.attempts USING (attempt_id)
            WHERE context = 'branding' AND state = 'completed';
            """), ct);
        brandingBatches.ShouldBe(2);
        var updates = await scenario.Read<long>(new NpgsqlCommand("""
            SELECT (SELECT sum(updates) FROM migration_fixture.backfill)
                + (SELECT sum(updates) FROM branding_fixture.backfill);
            """), ct);
        updates.ShouldBe(6);
        await using var catalogOwner = await scenario.OpenMigrationOwner(asBranding: false, ct);
        await using var brandingOwner = await scenario.OpenMigrationOwner(asBranding: true, ct);
        await using var brandingData = await scenario.OpenMigrationData(asBranding: true, ct);
        await using var catalogCrossRead = new NpgsqlCommand("SELECT count(*) FROM governed_migration_branding.attempts;", catalogOwner);
        await using var brandingCrossRead = catalogUsesDefault
            ? new NpgsqlCommand("SELECT count(*) FROM governed_migration.attempts;", brandingOwner)
            : new NpgsqlCommand("SELECT count(*) FROM governed_migration_catalog.attempts;", brandingOwner);
        await using var brandingDataRead = new NpgsqlCommand("SELECT count(*) FROM governed_migration_branding.batches;", brandingData);
        Func<Task> readBranding = () => catalogCrossRead.ExecuteScalarAsync(ct);
        Func<Task> readCatalog = () => brandingCrossRead.ExecuteScalarAsync(ct);
        Func<Task> readOwnDataJournal = () => brandingDataRead.ExecuteScalarAsync(ct);
        var catalogDenied = await readBranding.ShouldThrow<PostgresException>();
        var brandingDenied = await readCatalog.ShouldThrow<PostgresException>();
        var dataDenied = await readOwnDataJournal.ShouldThrow<PostgresException>();
        catalogDenied.SqlState.ShouldBe("42501");
        brandingDenied.SqlState.ShouldBe("42501");
        dataDenied.SqlState.ShouldBe("42501");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rejects_a_named_journal_without_the_selected_owner_provisioning_it(bool wrongOwner)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        if (wrongOwner)
        {
            await scenario.ProvisionSharedBranding(ct);
        }

        var manifest = await scenario.WriteManifest("missing-journal", [new("verify", "SELECT true;")], ct,
            journalSchema: "governed_migration_catalog");

        // Act
        var result = await scenario.Run(manifest, ct, asBranding: wrongOwner);

        // Assert
        result.ExitCode.ShouldBe(1);
        result.Error.ShouldContain("journal schema must be provisioned and owned", StringComparison.Ordinal);
        var defaultCreated = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration.attempts') IS NOT NULL;"), ct);
        defaultCreated.ShouldBeFalse();
    }

    [Fact]
    public async Task Keeps_an_existing_default_history_bound_to_its_context()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionSharedBranding(ct);
        var first = await scenario.WriteManifest("original-journal", [new("verify", "SELECT true;")], ct);
        var changed = await scenario.WriteManifest("changed-journal", [new("verify", "SELECT true;")], ct,
            journalSchema: "governed_migration_catalog");

        // Act
        var firstResult = await scenario.Run(first, ct);
        var pretendedSwitch = await scenario.Run(changed, ct);
        var retry = await scenario.Run(first, ct);

        // Assert
        firstResult.ExitCode.ShouldBe(0);
        pretendedSwitch.ExitCode.ShouldBe(1);
        pretendedSwitch.Error.ShouldContain("context already has a default migration journal", StringComparison.Ordinal);
        retry.Output.ShouldContain("verify 1 already completed", StringComparison.Ordinal);
        var selectedCreated = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration_catalog.attempts') IS NOT NULL;"), ct);
        selectedCreated.ShouldBeFalse();
    }

    [Fact]
    public async Task Refuses_to_fall_back_after_a_context_has_a_scoped_journal()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = new MigrationExecutionPostgreSqlScenario(await fixture.CreateDatabase(ct));
        await scenario.Provision(ct);
        await scenario.ProvisionSharedBranding(ct);
        var first = await scenario.WriteManifest("scoped-journal", [new("verify", "SELECT true;")], ct,
            journalSchema: "governed_migration_catalog");
        var fallback = await scenario.WriteManifest("implicit-journal", [new("verify", "SELECT true;")], ct);

        // Act
        var firstResult = await scenario.Run(first, ct);
        var wrongRoute = await scenario.Run(fallback, ct);
        var retry = await scenario.Run(first, ct);

        // Assert
        firstResult.ExitCode.ShouldBe(0);
        wrongRoute.ExitCode.ShouldBe(1);
        wrongRoute.Error.ShouldContain("context already has a scoped migration journal", StringComparison.Ordinal);
        retry.Output.ShouldContain("verify 1 already completed", StringComparison.Ordinal);
        var fallbackCreated = await scenario.Read<bool>(new NpgsqlCommand("SELECT to_regclass('governed_migration.attempts') IS NOT NULL;"), ct);
        fallbackCreated.ShouldBeFalse();
    }
}
