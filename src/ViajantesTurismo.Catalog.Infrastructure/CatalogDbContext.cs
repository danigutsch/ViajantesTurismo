using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Domain.PublicTheme;
using ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// EF Core context for Catalog persisted models.
/// </summary>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets editable public content entries.
    /// </summary>
    public DbSet<EditablePublicContent> PublicContent => Set<EditablePublicContent>();

    /// <summary>
    /// Gets editable public theme settings.
    /// </summary>
    public DbSet<PublicThemeSettings> PublicThemeSettings => Set<PublicThemeSettings>();

    internal DbSet<CatalogTourReadModelEntity> CatalogTourReadModels => Set<CatalogTourReadModelEntity>();

    internal DbSet<PublicMediaImage> PublicMediaImages => Set<PublicMediaImage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new EditablePublicContentConfiguration());
        modelBuilder.ApplyConfiguration(new PublicThemeSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogTourReadModelEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PublicMediaImageConfiguration());
    }
}
