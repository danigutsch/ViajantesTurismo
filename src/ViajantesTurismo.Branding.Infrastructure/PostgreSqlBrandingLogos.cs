using Microsoft.EntityFrameworkCore;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Coordinates immutable logo insertion and orphan reclamation on PostgreSQL.</summary>
internal static class PostgreSqlBrandingLogos
{
    private const string LogoTable = BrandingDbContext.SchemaName + ".\"BrandingLogos\"";

    private const string InsertLogo = $$"""
        INSERT INTO {{LogoTable}} ("Sha256", "ContentType", "Content", "StoredAt")
        VALUES ({0}, {1}, {2}, {3})
        ON CONFLICT ("Sha256") DO NOTHING
        """;

    private const string LockLogo = $$"""
        SELECT true AS "Value"
        FROM {{LogoTable}}
        WHERE "Sha256" = {0}
        FOR KEY SHARE
        """;

    private const string DeleteOrphans = $$"""
        WITH candidates AS (
            SELECT logo."Sha256"
            FROM {{LogoTable}} AS logo
            WHERE logo."StoredAt" < {0}
              AND NOT EXISTS (
                  SELECT 1 FROM {{BrandingDbContext.SchemaName}}."BrandingSettings" current_settings
                  WHERE current_settings."LogoSha256" = logo."Sha256")
              AND NOT EXISTS (
                  SELECT 1 FROM {{BrandingDbContext.SchemaName}}."BrandingSettingsRevisions" revision
                  WHERE revision."LogoSha256" = logo."Sha256")
            ORDER BY logo."StoredAt", logo."Sha256"
            LIMIT {1}
            FOR UPDATE OF logo SKIP LOCKED
        )
        DELETE FROM {{LogoTable}} AS logo
        USING candidates
        WHERE logo."Sha256" = candidates."Sha256"
        """;

    /// <summary>Inserts or reuses a logo and locks it until the transaction commits its references.</summary>
    internal static async Task InsertOrReuse(BrandingDbContext context, BrandingLogo logo, DateTimeOffset storedAt, CancellationToken ct)
    {
        var parameters = new object[] { logo.Sha256, logo.ContentType, logo.Content.ToArray(), storedAt };
        do
        {
            await context.Database.ExecuteSqlRawAsync(InsertLogo, parameters, ct).ConfigureAwait(false);
            // A collector can delete an old orphan between a conflict and the key lock. Retry with a fresh insert.
        }
        while (!await context.Database.SqlQueryRaw<bool>(LockLogo, logo.Sha256).SingleOrDefaultAsync(ct).ConfigureAwait(false));
    }

    /// <summary>Deletes one batch of old logos that are referenced by neither current settings nor revision history.</summary>
    internal static Task<int> Reclaim(BrandingDbContext context, DateTimeOffset cutoff, int batchSize, CancellationToken ct) =>
        context.Database.ExecuteSqlRawAsync(DeleteOrphans, [cutoff, batchSize], ct);
}
