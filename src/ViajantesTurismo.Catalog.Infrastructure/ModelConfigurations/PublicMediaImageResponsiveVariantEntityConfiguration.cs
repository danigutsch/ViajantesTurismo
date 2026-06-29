using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class PublicMediaImageResponsiveVariantEntityConfiguration
    : IEntityTypeConfiguration<PublicMediaImageResponsiveVariantEntity>
{
    public void Configure(EntityTypeBuilder<PublicMediaImageResponsiveVariantEntity> entity)
    {
        entity.ToTable("PublicMediaImageResponsiveVariants");
        entity.HasKey(variant => new { variant.PublicMediaImageId, variant.SortOrder });

        entity.Property(variant => variant.Uri).IsRequired();
        entity.Property(variant => variant.Width).IsRequired();
        entity.Property(variant => variant.Height).IsRequired();
        entity.Property(variant => variant.ContentType).HasMaxLength(ContractConstants.MaxContentTypeLength).IsRequired();
        entity.Property(variant => variant.FileSizeBytes).IsRequired();
    }
}
