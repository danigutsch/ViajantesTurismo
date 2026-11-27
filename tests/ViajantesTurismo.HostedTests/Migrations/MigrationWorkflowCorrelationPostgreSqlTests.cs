using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Rehearses governed workflow metadata expansions against the exact EF owner histories.</summary>
[TestScope(TestScope.Integration)]
public sealed class MigrationWorkflowCorrelationPostgreSqlTests(PostgreSqlTestServerFixture fixture)
    : IClassFixture<PostgreSqlTestServerFixture>
{
    private const string AdminPredecessor = "20261125144937_AddDocumentMaintenanceScheduler";
    private const string AdminTarget = "20261126125354_AddAdminWorkflowCorrelation";
    private const string CatalogPredecessor = "20261124091141_ObserveCatalogCompletedInboxDuplicates";
    private const string CatalogTarget = "20261126125355_AddCatalogWorkflowCorrelation";
    private const string BrandingPredecessor = "20261118105700_BoundBrandingLogoStorage";
    private const string BrandingTarget = "20261126125357_AddBrandingWorkflowCorrelation";

    [Fact]
    public async Task Governed_Admin_workflow_expansion_matches_EF_and_rejects_index_order_drift()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var database = await fixture.CreateDatabase(ct);
        await using var scope = new MigrationExecutionPostgreSqlScenario(database);
        var manifest = await MigrationBaselineFixture.WriteWorkflowCorrelationManifest(scope, "admin", ct);
        var verifier = await MigrationBaselineFixture.ReadExpansionScript("admin-workflow-correlation-verify", ct);
        var drift = await scope.WriteManifest("admin-workflow-correlation-drift", [new("verify", verifier)], ct, context: "admin");

        await using var expected = await fixture.CreateDatabase(ct);
        await scope.Provision(ct);
        await using var predecessor = MigrationBaselineFixture.CreateContext("admin", database.ConnectionString);
        await using var ef = MigrationBaselineFixture.CreateContext("admin", expected.ConnectionString);
        await predecessor.GetService<IMigrator>().MigrateAsync(AdminPredecessor, ct);
        await ef.GetService<IMigrator>().MigrateAsync(AdminPredecessor, ct);
        var originalShape = await MigrationBaselineSchema.Read(database.ConnectionString, ct);
        var expectedPredecessorShape = await MigrationBaselineSchema.Read(expected.ConnectionString, ct);
        await ef.GetService<IMigrator>().MigrateAsync(AdminTarget, ct);
        await scope.GrantWorkflowCorrelationExpansion(asBranding: false, ct);

        // Act
        var (expansionCode, _, expansionError) = await scope.Run(manifest, ct);
        var expandedShape = await MigrationBaselineSchema.Read(database.ConnectionString, ct);
        var (retryCode, retryOutput, retryError) = await scope.Run(manifest, ct);
        var retriedShape = await MigrationBaselineSchema.Read(database.ConnectionString, ct);

        // Assert
        expansionError.ShouldBe(string.Empty);
        expansionCode.ShouldBe(0);
        retryError.ShouldBe(string.Empty);
        retryCode.ShouldBe(0);
        retryOutput.ShouldContain("expand 1 already completed", StringComparison.Ordinal);
        retryOutput.ShouldContain("verify 2 already completed", StringComparison.Ordinal);
        originalShape.ShouldBe(expectedPredecessorShape);
        expandedShape.ShouldNotBe(originalShape);
        retriedShape.ShouldBe(expandedShape);
        retriedShape.ShouldBe(await MigrationBaselineSchema.Read(expected.ConnectionString, ct));
        (await predecessor.Database.GetAppliedMigrationsAsync(ct)).ToArray().ShouldBe(
            (await ef.Database.GetAppliedMigrationsAsync(ct)).ToArray());

        // A fresh manifest reruns verification after the correctly named index is replaced.
        await using var owner = await scope.OpenMigrationOwner(asBranding: false, ct);
        await using var replace = new NpgsqlCommand("""
            DROP INDEX messaging."IX_outbox_messages_WorkflowCorrelationId_EnqueuedAt_Id";
            CREATE INDEX "IX_outbox_messages_WorkflowCorrelationId_EnqueuedAt_Id"
                ON messaging.outbox_messages ("WorkflowCorrelationId", "EnqueuedAt" ASC, "Id" DESC);
            """, owner);
        await replace.ExecuteNonQueryAsync(ct);
        var (driftCode, _, driftError) = await scope.Run(drift, ct);
        driftCode.ShouldBe(1);
        driftError.ShouldContain("verify 1 failed", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Governed_Catalog_and_Branding_workflow_expansions_match_EF_with_separate_owners()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var database = await fixture.CreateDatabase(ct);
        await using var scope = new MigrationExecutionPostgreSqlScenario(database);
        var catalog = await MigrationBaselineFixture.WriteWorkflowCorrelationManifest(scope, "catalog", ct, "governed_migration_catalog");
        var branding = await MigrationBaselineFixture.WriteWorkflowCorrelationManifest(scope, "branding", ct, "governed_migration_branding");

        await using var expected = await fixture.CreateDatabase(ct);
        await scope.Provision(ct);
        await scope.ProvisionSharedBranding(ct);
        await using var catalogPredecessor = MigrationBaselineFixture.CreateContext("catalog", database.ConnectionString);
        await using var brandingPredecessor = MigrationBaselineFixture.CreateContext("branding", database.ConnectionString);
        await using var efCatalog = MigrationBaselineFixture.CreateContext("catalog", expected.ConnectionString);
        await using var efBranding = MigrationBaselineFixture.CreateContext("branding", expected.ConnectionString);
        await catalogPredecessor.GetService<IMigrator>().MigrateAsync(CatalogPredecessor, ct);
        await brandingPredecessor.GetService<IMigrator>().MigrateAsync(BrandingPredecessor, ct);
        await efCatalog.GetService<IMigrator>().MigrateAsync(CatalogPredecessor, ct);
        await efBranding.GetService<IMigrator>().MigrateAsync(BrandingPredecessor, ct);
        var predecessorShape = await MigrationBaselineSchema.Read(database.ConnectionString, ct);
        var expectedPredecessorShape = await MigrationBaselineSchema.Read(expected.ConnectionString, ct);
        var predecessorBrandingHistory = (await brandingPredecessor.Database.GetAppliedMigrationsAsync(ct)).ToArray();
        await scope.GrantWorkflowCorrelationExpansion(asBranding: false, ct);
        await scope.GrantWorkflowCorrelationExpansion(asBranding: true, ct);

        // Act
        await efCatalog.GetService<IMigrator>().MigrateAsync(CatalogTarget, ct);
        var (catalogCode, _, catalogError) = await scope.Run(catalog, ct);
        var (catalogRetryCode, catalogRetryOutput, catalogRetryError) = await scope.Run(catalog, ct);
        var catalogOnlyShape = await MigrationBaselineSchema.Read(database.ConnectionString, ct);
        var expectedCatalogOnlyShape = await MigrationBaselineSchema.Read(expected.ConnectionString, ct);
        var untouchedBrandingHistory = (await brandingPredecessor.Database.GetAppliedMigrationsAsync(ct)).ToArray();

        await efBranding.GetService<IMigrator>().MigrateAsync(BrandingTarget, ct);
        var (brandingCode, _, brandingError) = await scope.Run(branding, ct, asBranding: true);
        var (brandingRetryCode, brandingRetryOutput, brandingRetryError) = await scope.Run(branding, ct, asBranding: true);
        var finalShape = await MigrationBaselineSchema.Read(database.ConnectionString, ct);

        // Assert
        catalogError.ShouldBe(string.Empty);
        catalogCode.ShouldBe(0);
        catalogRetryError.ShouldBe(string.Empty);
        catalogRetryCode.ShouldBe(0);
        catalogRetryOutput.ShouldContain("expand 1 already completed", StringComparison.Ordinal);
        catalogRetryOutput.ShouldContain("verify 2 already completed", StringComparison.Ordinal);
        brandingError.ShouldBe(string.Empty);
        brandingCode.ShouldBe(0);
        brandingRetryError.ShouldBe(string.Empty);
        brandingRetryCode.ShouldBe(0);
        brandingRetryOutput.ShouldContain("expand 1 already completed", StringComparison.Ordinal);
        brandingRetryOutput.ShouldContain("verify 2 already completed", StringComparison.Ordinal);

        predecessorShape.ShouldBe(expectedPredecessorShape);
        catalogOnlyShape.ShouldBe(expectedCatalogOnlyShape);
        untouchedBrandingHistory.ShouldBe(predecessorBrandingHistory);
        finalShape.ShouldBe(await MigrationBaselineSchema.Read(expected.ConnectionString, ct));
        (await catalogPredecessor.Database.GetAppliedMigrationsAsync(ct)).ToArray().ShouldBe(
            (await efCatalog.Database.GetAppliedMigrationsAsync(ct)).ToArray());
        (await brandingPredecessor.Database.GetAppliedMigrationsAsync(ct)).ToArray().ShouldBe(
            (await efBranding.Database.GetAppliedMigrationsAsync(ct)).ToArray());
    }
}
