using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class CatalogTourReadModelEntityConfiguration : IEntityTypeConfiguration<CatalogTourReadModelEntity>
{
    public void Configure(EntityTypeBuilder<CatalogTourReadModelEntity> entity)
    {
        entity.ToTable("CatalogTourReadModels");
        entity.HasKey(tour => tour.CatalogTourId);
        entity.HasIndex(tour => tour.AdminTourId).IsUnique();
        entity.HasIndex(tour => tour.Slug).IsUnique();

        entity.Property(tour => tour.Identifier).HasMaxLength(CatalogDomainLimits.MaxDefaultLength).IsRequired();
        entity.Property(tour => tour.Title).HasMaxLength(CatalogDomainLimits.MaxNameLength).IsRequired();
        entity.Property(tour => tour.Slug).HasMaxLength(CatalogDomainLimits.MaxSlugLength).IsRequired();
        entity.Property(tour => tour.UpdatedAt).IsRequired();
    }
}
