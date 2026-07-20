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
        entity.Property(tour => tour.Summary).HasMaxLength(CatalogDomainLimits.MaxBodyLength).IsRequired();
        entity.Property(tour => tour.Description).HasMaxLength(CatalogDomainLimits.MaxBodyLength).IsRequired();
        entity.Property(tour => tour.Itinerary).HasMaxLength(CatalogDomainLimits.MaxBodyLength).IsRequired();
        entity.Property(tour => tour.SeoTitle).HasMaxLength(CatalogDomainLimits.MaxNameLength).IsRequired();
        entity.Property(tour => tour.SeoDescription).HasMaxLength(CatalogDomainLimits.MaxBodyLength).IsRequired();
        entity.Property(tour => tour.StreamVersion).HasDefaultValue(1L).IsRequired();
        entity.Property(tour => tour.PresentationPosition).IsRequired().IsConcurrencyToken();
        entity.Property(tour => tour.PublicationPosition).IsRequired().IsConcurrencyToken();
        entity.Property(tour => tour.Position).IsConcurrencyToken();
        entity.Property(tour => tour.UpdatedAt).IsRequired();
    }
}
