using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class PublicMediaImageTourLinkEntityConfiguration : IEntityTypeConfiguration<PublicMediaImageTourLinkEntity>
{
    public void Configure(EntityTypeBuilder<PublicMediaImageTourLinkEntity> entity)
    {
        entity.ToTable("PublicMediaImageTourLinks");
        entity.HasKey(link => new { link.PublicMediaImageId, link.CatalogTourId });
        entity.HasIndex(link => new { link.CatalogTourId, link.DisplayOrder });

        entity.Property(link => link.DisplayOrder).IsRequired();
        entity.Property(link => link.IsCover).IsRequired();
    }
}
