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

    private static async Task<string> ReadScript(string asset, CancellationToken ct)
    {
        return await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "MigrationBaselines", asset + ".sql"), ct);
    }
}
