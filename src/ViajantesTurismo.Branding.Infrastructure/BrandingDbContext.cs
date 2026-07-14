using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using ViajantesTurismo.Branding.Infrastructure.ModelConfigurations;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>
/// EF Core context for Branding persisted models.
/// </summary>
public sealed class BrandingDbContext(
    DbContextOptions<BrandingDbContext> options,
    IEnumerable<IDbContextConfiguration<BrandingDbContext>>? configurations = null) : DbContext(options)
{
    internal const string SchemaName = "branding";
    internal const string MigrationsHistorySchemaName = "public";

    internal DbSet<BrandingSettingsRecord> BrandingSettings => Set<BrandingSettingsRecord>();

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

        if (configurations is not null)
        {
            foreach (var configuration in configurations)
            {
                configuration.ConfigureModel(modelBuilder);
            }
        }
    }
}
