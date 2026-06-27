using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Domain.PublicContent;
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

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new EditablePublicContentConfiguration());
    }
}
