using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class CatalogTourReadModelEntityConfiguration : IEntityTypeConfiguration<CatalogTourReadModelEntity>
{
    public void Configure(EntityTypeBuilder<CatalogTourReadModelEntity> entity)
    {
        entity.ToTable("CatalogTourReadModels");
        entity.HasKey(tour => tour.CatalogTourId);
        entity.HasIndex(tour => tour.AdminTourId).IsUnique();
        entity.HasIndex(tour => tour.Slug).IsUnique();

        entity.Property(tour => tour.Identifier).HasMaxLength(ContractConstants.MaxDefaultLength).IsRequired();
        entity.Property(tour => tour.Title).HasMaxLength(ContractConstants.MaxNameLength).IsRequired();
        entity.Property(tour => tour.Slug).HasMaxLength(ContractConstants.MaxSlugLength).IsRequired();
        entity.Property(tour => tour.UpdatedAt).IsRequired();
    }
}
