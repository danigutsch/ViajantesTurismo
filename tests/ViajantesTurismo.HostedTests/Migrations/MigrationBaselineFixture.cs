using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.HostedTests.Migrations;

/// <summary>Pairs each governed rehearsal script with its actual application DbContext.</summary>
internal static class MigrationBaselineFixture
{
    /// <summary>Creates the owning design-time context at a disposable database.</summary>
    internal static DbContext CreateContext(string context, string connectionString)
    {
        DbContext database = context switch
        {
            "admin" => new AdminWriteDbContextDesignTimeFactory().CreateDbContext([]),
            "catalog" => new CatalogDbContextDesignTimeFactory().CreateDbContext([]),
            "branding" => new BrandingDbContextDesignTimeFactory().CreateDbContext([]),
            "management-security" => new ManagementSecurityDbContextFactory().CreateDbContext([]),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, "Unknown baseline context."),
        };
        database.Database.SetConnectionString(connectionString);
        return database;
    }

    /// <summary>Writes a synthetic promotion manifest that executes the repository baseline bytes.</summary>
    internal static async Task<string> WriteManifest(
        MigrationExecutionPostgreSqlScenario scope, string context, CancellationToken ct, string journalSchema = "")
    {
        var sql = await ReadScript(context, ct);
        var steps = new List<MigrationExecutionTestStep>
        {
            new("expand", sql, TimeoutSeconds: 60, LockTimeoutSeconds: 5)
        };
        var table = context == "branding" ? "__EFMigrationsHistory_Branding" : "__EFMigrationsHistory";
        var verify = $"""SELECT pg_catalog.to_regclass('public."{table}"') IS NOT NULL;""";
        if (context == "catalog")
        {
            steps.Add(new("expand", await ReadScript("catalog-event-store", ct), TimeoutSeconds: 60, LockTimeoutSeconds: 5));
            verify = """
                SELECT pg_catalog.to_regclass('public."__EFMigrationsHistory"') IS NOT NULL
                   AND pg_catalog.to_regclass('event_sourcing.events') IS NOT NULL;
                """;
        }

        steps.Add(new("verify", verify, TimeoutSeconds: 10, LockTimeoutSeconds: 5));
        return await scope.WriteManifest("baseline-" + context, steps, ct, context: context, journalSchema: journalSchema);
    }

    /// <summary>Reads the maintained PostgreSQL baseline asset included by the test host.</summary>
    internal static async Task<string> ReadScript(string asset, CancellationToken ct)
    {
        return await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "MigrationBaselines", asset + ".sql"), ct);
    }

    /// <summary>Writes a test manifest with the reviewed Catalog causation expansion and verification assets.</summary>
    internal static async Task<string> WriteCatalogCausationManifest(MigrationExecutionPostgreSqlScenario scope, CancellationToken ct)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MigrationExpansions");
        var expand = await File.ReadAllTextAsync(Path.Combine(directory, "catalog-event-causation.sql"), ct);
        var verify = await ReadCatalogCausationVerification(ct);
        return await scope.WriteManifest("catalog-event-causation",
            [new("expand", expand), new("verify", verify)], ct, journalSchema: "governed_migration_catalog");
    }

    /// <summary>Reads the maintained Catalog event causation verification asset.</summary>
    internal static Task<string> ReadCatalogCausationVerification(CancellationToken ct) =>
        File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "MigrationExpansions", "catalog-event-causation-verify.sql"), ct);

    /// <summary>Writes a test manifest for the maintained Admin scheduler assets.</summary>
    internal static async Task<string> WriteAdminSchedulerManifest(MigrationExecutionPostgreSqlScenario scope, CancellationToken ct)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "MigrationExpansions");
        var expand = await File.ReadAllTextAsync(Path.Combine(directory, "admin-document-maintenance-scheduler.sql"), ct);
        var verify = await File.ReadAllTextAsync(Path.Combine(directory, "admin-document-maintenance-scheduler-verify.sql"), ct);
        return await scope.WriteManifest("admin-document-maintenance-scheduler",
            [new("expand", expand), new("verify", verify)], ct, context: "admin");
    }

    /// <summary>Writes a synthetic manifest using the selected owner's reviewed workflow expansion and verification.</summary>
    internal static async Task<string> WriteWorkflowCorrelationManifest(
        MigrationExecutionPostgreSqlScenario scope, string context, CancellationToken ct, string journalSchema = "")
    {
        var migrationId = context + "-workflow-correlation";
        var expand = await ReadExpansionScript(migrationId, ct);
        var verify = await ReadExpansionScript(migrationId + "-verify", ct);
        return await scope.WriteManifest(migrationId, [new("expand", expand), new("verify", verify)],
            ct, context: context, journalSchema: journalSchema);
    }

    /// <summary>Reads a maintained expansion asset copied into the test host.</summary>
    internal static Task<string> ReadExpansionScript(string asset, CancellationToken ct) =>
        File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "MigrationExpansions", asset + ".sql"), ct);
}
