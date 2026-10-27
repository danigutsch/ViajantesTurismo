using Microsoft.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using ViajantesTurismo.Branding.Infrastructure.ModelConfigurations;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>
/// EF Core context for Branding persisted models.
/// </summary>
public sealed class BrandingDbContext(
    DbContextOptions<BrandingDbContext> options,
    IEnumerable<IDbContextConfiguration<BrandingDbContext>>? configurations = null) : DbContext(options)
{
    /// <summary>Database schema that holds Branding tables.</summary>
    internal const string SchemaName = "branding";
    /// <summary>Schema that holds the EF migrations history table.</summary>
    internal const string MigrationsHistorySchemaName = "public";

    /// <summary>Current Branding settings row.</summary>
    internal DbSet<BrandingSettingsRecord> BrandingSettings => Set<BrandingSettingsRecord>();

    /// <summary>Immutable history of committed Branding settings revisions.</summary>
    internal DbSet<BrandingSettingsRevisionRecord> BrandingSettingsRevisions => Set<BrandingSettingsRevisionRecord>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        if (configurations is not null)
        {
            foreach (var configuration in configurations)
            {
                configuration.ConfigureConventions(configurationBuilder);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfiguration(new BrandingSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new BrandingSettingsRevisionConfiguration());
        modelBuilder.ApplyConfiguration(new BrandingRecoveryAuditConfiguration());

        if (configurations is not null)
        {
            foreach (var configuration in configurations)
            {
                configuration.ConfigureModel(modelBuilder);
            }
        }
    }
}
