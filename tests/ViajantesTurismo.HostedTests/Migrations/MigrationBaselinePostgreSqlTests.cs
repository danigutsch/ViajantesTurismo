using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SharedKernel.EventSourcing.Npgsql;
using SharedKernel.Npgsql;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Documents.Maintenance;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Rehearses governed baselines alongside matching EF-owned application schemas.</summary>
[TestScope(TestScope.Integration)]
public sealed class MigrationBaselinePostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task The_fresh_baseline_rejects_an_initialized_target_and_preserves_its_history()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var database = await fixture.CreateDatabase(ct);
        await using var scope = new MigrationExecutionPostgreSqlScenario(database);
        await scope.Provision(ct);
        await scope.GrantBaselineCreation(asBranding: false, ct);
        await using var existing = MigrationBaselineFixture.CreateContext("management-security", database.ConnectionString);
        await existing.GetService<IMigrator>().MigrateAsync("20261111091545_InitialManagementSecurity", ct);
        var history = (await existing.Database.GetAppliedMigrationsAsync(ct)).ToArray();
        var original = await MigrationBaselineSchema.Read(database.ConnectionString, ct);
        var manifest = await MigrationBaselineFixture.WriteManifest(scope, "management-security", ct);

        // Act
        var baseline = await scope.Run(manifest, ct);
        var after = await MigrationBaselineSchema.Read(database.ConnectionString, ct);

        // Assert
        baseline.ExitCode.ShouldBe(1);
        baseline.Error.ShouldContain("expand 1 failed", StringComparison.Ordinal);
        (await existing.Database.GetAppliedMigrationsAsync(ct)).ToArray().ShouldBe(history);
        after.ShouldBe(original);
    }

    [Theory]
    [InlineData("admin", "20261119182941_RecordFinalizedArtifactProvenance")]
    [InlineData("management-security", "20261111091545_InitialManagementSecurity")]
    public async Task The_governed_baseline_matches_the_EF_schema_and_history(string context, string target)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var expected = await fixture.CreateDatabase(ct);
        var database = await fixture.CreateDatabase(ct);
        await using var scope = new MigrationExecutionPostgreSqlScenario(database);
        await scope.Provision(ct);
        await scope.GrantBaselineCreation(asBranding: false, ct);
        await using var ef = MigrationBaselineFixture.CreateContext(context, expected.ConnectionString);
        await ef.GetService<IMigrator>().MigrateAsync(target, ct);
        var manifest = await MigrationBaselineFixture.WriteManifest(scope, context, ct);

        // Act
        var baseline = await scope.Run(manifest, ct);
        var repeated = await scope.Run(manifest, ct);
        await using var applied = MigrationBaselineFixture.CreateContext(context, database.ConnectionString);
        var history = (await applied.Database.GetAppliedMigrationsAsync(ct)).ToArray();
        var shape = await MigrationBaselineSchema.Read(database.ConnectionString, ct);

        // Assert
        baseline.Error.ShouldBe(string.Empty);
        baseline.ExitCode.ShouldBe(0);
        repeated.Error.ShouldBe(string.Empty);
        repeated.ExitCode.ShouldBe(0);
        history.ShouldBe((await ef.Database.GetAppliedMigrationsAsync(ct)).ToArray());
        shape.ShouldBe(await MigrationBaselineSchema.Read(expected.ConnectionString, ct));
    }

    [Fact]
    public async Task The_governed_maintenance_expansion_matches_EF_and_keeps_runtime_audit_append_only()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var expected = await fixture.CreateDatabase(ct);
        var database = await fixture.CreateDatabase(ct);
        await using var scope = new MigrationExecutionPostgreSqlScenario(database);
        await scope.Provision(ct);
        await scope.GrantBaselineCreation(asBranding: false, ct);
        await using var predecessor = MigrationBaselineFixture.CreateContext("admin", database.ConnectionString);
        await predecessor.GetService<IMigrator>().MigrateAsync("20261123164511_IndexPaymentAuditsByTargetAndSequence", ct);
        await scope.GrantAdminHistoryAppend(ct);
        await using var ef = MigrationBaselineFixture.CreateContext("admin", expected.ConnectionString);
        await ef.Database.MigrateAsync(ct);
        var manifest = await MigrationBaselineFixture.WriteAdminSchedulerManifest(scope, ct);

        // Act
        var (expandCode, _, expandError) = await scope.Run(manifest, ct);
        var (repeatCode, _, repeatError) = await scope.Run(manifest, ct);
        await scope.ProvisionAdminSchedulerRuntime(ct);
        await using var dataSource = NpgsqlDataSource.Create(scope.RuntimeConnectionString);
        var scheduler = new PostgreSqlJobScheduler(dataSource, DocumentMaintenanceSchedule.DurableSchema);
        var jobName = DocumentMaintenanceSchedule.GetJobName(DocumentMaintenanceJob.Drafts);
        await scheduler.Register(jobName, TimeProvider.System.GetUtcNow().AddDays(1), DocumentMaintenanceSchedule.FixedDelay, ct);
        var result = await PostgreSqlDocumentStoreScenario.ControlMaintenanceAt(
            scope.RuntimeConnectionString,
            new ControlDocumentMaintenanceCommand(DocumentMaintenanceJob.Drafts, DocumentMaintenanceControlAction.Pause,
                new CreateDocumentMaintenanceControlDto
                {
                    RequestId = Guid.CreateVersion7(),
                    Confirmed = true,
                    Reason = "Synthetic migration rehearsal."
                },
                "synthetic-migration-operator"),
            ct);
        await using var runtime = await scope.OpenRuntime(ct);
        await using var delete = new NpgsqlCommand("DELETE FROM document_maintenance.control_audit;", runtime);
        Func<Task> deleteAsRuntime = () => delete.ExecuteNonQueryAsync(ct);
        var denied = await deleteAsRuntime.ShouldThrow<PostgresException>();

        // Assert
        expandError.ShouldBe(string.Empty);
        expandCode.ShouldBe(0);
        repeatError.ShouldBe(string.Empty);
        repeatCode.ShouldBe(0);
        result.IsSuccess.ShouldBeTrue();
        (await scheduler.ReadJob(jobName, ct)).ShouldNotBeNull().IsPaused.ShouldBeTrue();
        denied.SqlState.ShouldBe(PostgresErrorCodes.InsufficientPrivilege);
        (await predecessor.Database.GetAppliedMigrationsAsync(ct)).ToArray().ShouldBe(
            (await ef.Database.GetAppliedMigrationsAsync(ct)).ToArray());
        (await MigrationBaselineSchema.Read(database.ConnectionString, ct)).ShouldBe(
            await MigrationBaselineSchema.Read(expected.ConnectionString, ct));
    }

    [Fact]
    public async Task The_EF_scheduler_rollback_removes_its_additions_and_restores_the_predecessor()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var ef = MigrationBaselineFixture.CreateContext("admin", database.ConnectionString);
        const string predecessor = "20261123164511_IndexPaymentAuditsByTargetAndSequence";
        var migrator = ef.GetService<IMigrator>();
        await migrator.MigrateAsync(predecessor, ct);
        var before = await MigrationBaselineSchema.Read(database.ConnectionString, ct);
        var history = (await ef.Database.GetAppliedMigrationsAsync(ct)).ToArray();

        // Act
        await migrator.MigrateAsync(targetMigration: null, ct);
        var expanded = await MigrationBaselineSchema.Read(database.ConnectionString, ct);
        await migrator.MigrateAsync(predecessor, ct);
        var contracted = await MigrationBaselineSchema.Read(database.ConnectionString, ct);

        // Assert
        expanded.ShouldNotBe(before);
        contracted.ShouldBe(before);
        (await ef.Database.GetAppliedMigrationsAsync(ct)).ToArray().ShouldBe(history);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task The_catalog_causation_verifier_rejects_a_view_with_the_expected_column(bool materialized)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var database = await fixture.CreateDatabase(ct);
        await using var scope = new MigrationExecutionPostgreSqlScenario(database);
        await scope.Provision(ct);
        await scope.ProvisionCatalogEventStoreSchema(ct);
        await using var owner = await scope.OpenMigrationOwner(asBranding: false, ct);
        await using var createView = materialized
            ? new NpgsqlCommand("CREATE MATERIALIZED VIEW event_sourcing.events AS SELECT NULL::uuid AS causation_event_id;", owner)
            : new NpgsqlCommand("CREATE VIEW event_sourcing.events AS SELECT NULL::uuid AS causation_event_id;", owner);
        await createView.ExecuteNonQueryAsync(ct);
        var verify = await MigrationBaselineFixture.ReadCatalogCausationVerification(ct);
        var manifest = await scope.WriteManifest("catalog-causation-verification-view", [new("verify", verify)], ct);

        // Act
        var result = await scope.Run(manifest, ct);

        // Assert
        result.ExitCode.ShouldBe(1);
        result.Error.ShouldContain("verify", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Separate_owners_baseline_Catalog_and_Branding_in_one_database()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var expected = await fixture.CreateDatabase(ct);
        var database = await fixture.CreateDatabase(ct);
        await using var scope = new MigrationExecutionPostgreSqlScenario(database);
        await scope.Provision(ct);
        await scope.ProvisionSharedBranding(ct);
        await scope.ProvisionCatalogEventStoreSchema(ct);
        await scope.GrantBaselineCreation(asBranding: false, ct);
        await scope.GrantBaselineCreation(asBranding: true, ct);
        await using var efCatalog = MigrationBaselineFixture.CreateContext("catalog", expected.ConnectionString);
        await using var efBranding = MigrationBaselineFixture.CreateContext("branding", expected.ConnectionString);
        await efCatalog.GetService<IMigrator>().MigrateAsync("20261119170602_AddCatalogMediaUploadFingerprint", ct);
        await efBranding.GetService<IMigrator>().MigrateAsync("20261118105700_BoundBrandingLogoStorage", ct);
        await using var eventStore = NpgsqlDataSource.Create(expected.ConnectionString);
        await PostgreSqlEventSourcingSchema.Initialize(eventStore, options: null, ct);
        var catalog = await MigrationBaselineFixture.WriteManifest(scope, "catalog", ct, "governed_migration_catalog");
        var branding = await MigrationBaselineFixture.WriteManifest(scope, "branding", ct, "governed_migration_branding");
        var catalogResult = await scope.Run(catalog, ct);
        var brandingResult = await scope.Run(branding, ct, asBranding: true);
        var columnExists = await scope.Read<bool>(new NpgsqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'event_sourcing' AND table_name = 'events' AND column_name = 'causation_event_id');
            """), ct);
        await using var owner = await scope.OpenMigrationOwner(asBranding: false, ct);
        await using var seedLegacyEvent = new NpgsqlCommand("""
            INSERT INTO event_sourcing.event_streams (stream_id, stream_revision, created_at_utc, updated_at_utc)
            VALUES ('legacy-fixture', 1, '2026-11-01T09:00:00Z', '2026-11-01T09:00:00Z');

            INSERT INTO event_sourcing.events (event_id, stream_id, stream_revision, event_type, payload_json, recorded_at_utc)
            VALUES ('5b4e50ab-c022-48fd-b0ed-116dffcb8ce3', 'legacy-fixture', 1, 'fixture.legacy.v1',
                    '"legacy fixture payload"'::jsonb, '2026-11-01T09:00:00Z');
            """, owner);
        await seedLegacyEvent.ExecuteNonQueryAsync(ct);
        await scope.ProvisionCatalogEventRuntime(ct);
        await using var runtime = await scope.OpenRuntime(ct);
        await using var ddl = new NpgsqlCommand("ALTER TABLE event_sourcing.events ADD COLUMN forbidden integer;", runtime);
        Func<Task> alterAsRuntime = () => ddl.ExecuteNonQueryAsync(ct);
        var manifest = await MigrationBaselineFixture.WriteCatalogCausationManifest(scope, ct);

        // Act
        var denied = await alterAsRuntime.ShouldThrow<PostgresException>();
        var expanded = await scope.Run(manifest, ct);
        var retry = await scope.Run(manifest, ct);
        await using var readLegacyEvent = new NpgsqlCommand("""
            SELECT count(*) = 1 AND pg_catalog.bool_and(
                position = 1
                AND event_id = '5b4e50ab-c022-48fd-b0ed-116dffcb8ce3'
                AND stream_id = 'legacy-fixture'
                AND stream_revision = 1
                AND event_type = 'fixture.legacy.v1'
                AND payload_json = '"legacy fixture payload"'::jsonb
                AND recorded_at_utc = '2026-11-01T09:00:00Z'
                AND causation_event_id IS NULL)
            FROM event_sourcing.events;
            """, runtime);
        var retained = await readLegacyEvent.ExecuteScalarAsync(ct);
        await using var appliedCatalog = MigrationBaselineFixture.CreateContext("catalog", database.ConnectionString);
        await using var appliedBranding = MigrationBaselineFixture.CreateContext("branding", database.ConnectionString);
        var shape = await MigrationBaselineSchema.Read(database.ConnectionString, ct);

        // Assert
        catalogResult.Error.ShouldBe(string.Empty);
        catalogResult.ExitCode.ShouldBe(0);
        brandingResult.Error.ShouldBe(string.Empty);
        brandingResult.ExitCode.ShouldBe(0);
        columnExists.ShouldBeFalse();
        denied.SqlState.ShouldBe("42501");
        expanded.Error.ShouldBe(string.Empty);
        expanded.ExitCode.ShouldBe(0);
        retry.Error.ShouldBe(string.Empty);
        retry.ExitCode.ShouldBe(0);
        retained.ShouldBe(true);
        var attempts = await scope.Read<string>(new NpgsqlCommand("""
            SELECT string_agg(phase || ':' || state, ',' ORDER BY attempt_id)
            FROM governed_migration_catalog.attempts
            WHERE context = 'catalog' AND migration_id = 'catalog-event-causation'
              AND length(script_sha256) = 64 AND length(manifest_sha256) = 64;
            """), ct);
        attempts.ShouldBe("expand:completed,verify:completed");
        (await appliedCatalog.Database.GetAppliedMigrationsAsync(ct)).ToArray()
            .ShouldBe((await efCatalog.Database.GetAppliedMigrationsAsync(ct)).ToArray());
        (await appliedBranding.Database.GetAppliedMigrationsAsync(ct)).ToArray()
            .ShouldBe((await efBranding.Database.GetAppliedMigrationsAsync(ct)).ToArray());
        shape.ShouldBe(await MigrationBaselineSchema.Read(expected.ConnectionString, ct));
    }
}
