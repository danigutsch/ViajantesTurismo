using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using PandaBear.EventSourcing.Npgsql;
using PandaBear.Testing;

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

        // Act
        var catalogResult = await scope.Run(catalog, ct);
        var brandingResult = await scope.Run(branding, ct, asBranding: true);
        await using var appliedCatalog = MigrationBaselineFixture.CreateContext("catalog", database.ConnectionString);
        await using var appliedBranding = MigrationBaselineFixture.CreateContext("branding", database.ConnectionString);
        var shape = await MigrationBaselineSchema.Read(database.ConnectionString, ct);

        // Assert
        catalogResult.Error.ShouldBe(string.Empty);
        catalogResult.ExitCode.ShouldBe(0);
        brandingResult.Error.ShouldBe(string.Empty);
        brandingResult.ExitCode.ShouldBe(0);
        (await appliedCatalog.Database.GetAppliedMigrationsAsync(ct)).ToArray()
            .ShouldBe((await efCatalog.Database.GetAppliedMigrationsAsync(ct)).ToArray());
        (await appliedBranding.Database.GetAppliedMigrationsAsync(ct)).ToArray()
            .ShouldBe((await efBranding.Database.GetAppliedMigrationsAsync(ct)).ToArray());
        shape.ShouldBe(await MigrationBaselineSchema.Read(expected.ConnectionString, ct));
    }
}
