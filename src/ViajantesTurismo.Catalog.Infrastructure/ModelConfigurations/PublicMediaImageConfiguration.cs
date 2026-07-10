using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class PublicMediaImageConfiguration : IEntityTypeConfiguration<PublicMediaImage>
{
    public void Configure(EntityTypeBuilder<PublicMediaImage> entity)
    {
        entity.ToTable("PublicMediaImages");
        entity.HasKey(image => image.Id);
        entity.Property(image => image.Id).ValueGeneratedNever();

        entity.Property(image => image.SourceObjectKey).HasMaxLength(1024).IsRequired();
        entity.Property(image => image.Checksum).HasMaxLength(CatalogDomainLimits.MaxChecksumLength).IsRequired();
        entity.Property(image => image.ContentType).HasMaxLength(CatalogDomainLimits.MaxContentTypeLength).IsRequired();
        entity.Property(image => image.ProcessingStatus).HasConversion<string>().IsRequired();
        entity.Property<List<string>>("_tags").HasColumnName("Tags").IsRequired();
        entity.Property(image => image.AltText).HasMaxLength(CatalogDomainLimits.MaxAltTextLength).IsRequired();
        entity.Property(image => image.Caption).HasMaxLength(CatalogDomainLimits.MaxCaptionLength);
        entity.Property(image => image.IsDecorative).IsRequired();
        entity.Property(image => image.RequiresHumanReview).IsRequired();
        entity.Property(image => image.IsAiGenerated).IsRequired();
        entity.Property(image => image.Attribution).HasMaxLength(CatalogDomainLimits.MaxAttributionLength);
        entity.Property(image => image.Copyright).HasMaxLength(CatalogDomainLimits.MaxCopyrightLength);

        entity.OwnsOne(image => image.Dimensions, dimensions =>
        {
            dimensions.Property(dimension => dimension.Width).HasColumnName("Width").IsRequired();
            dimensions.Property(dimension => dimension.Height).HasColumnName("Height").IsRequired();
        });

        entity.OwnsMany(image => image.ResponsiveVariants, variant =>
        {
            variant.ToTable("PublicMediaImageResponsiveVariants");
            variant.WithOwner().HasForeignKey("PublicMediaImageId");
            variant.HasKey("PublicMediaImageId", nameof(MediaImageResponsiveVariant.SortOrder));
            variant.Property(item => item.SortOrder).ValueGeneratedNever();
            variant.Property(item => item.ObjectKey).HasMaxLength(1024).IsRequired();
            variant.Property(item => item.Width).IsRequired();
            variant.Property(item => item.Height).IsRequired();
            variant.Property(item => item.ContentType).HasMaxLength(CatalogDomainLimits.MaxContentTypeLength).IsRequired();
            variant.Property(item => item.FileSizeBytes).IsRequired();
        });

        entity.OwnsMany(image => image.TourLinks, link =>
        {
            link.ToTable("PublicMediaImageTourLinks");
            link.WithOwner().HasForeignKey("PublicMediaImageId");
            link.HasKey("PublicMediaImageId", nameof(MediaImageTourLink.CatalogTourId));
            link.HasIndex(item => new { item.CatalogTourId, item.DisplayOrder });
            link.Property(item => item.CatalogTourId).ValueGeneratedNever();
            link.Property(item => item.DisplayOrder).IsRequired();
            link.Property(item => item.IsCover).IsRequired();
        });

        entity.OwnsMany(image => image.AccessibilityTexts, text =>
        {
            text.ToTable("PublicMediaImageAccessibilityTexts");
            text.WithOwner().HasForeignKey("PublicMediaImageId");
            text.HasKey("PublicMediaImageId", nameof(PublicMediaImageAccessibilityText.Language));
            text.Property(item => item.Language).HasConversion<string>().IsRequired();
            text.Property(item => item.AltText).HasMaxLength(CatalogDomainLimits.MaxAltTextLength);
            text.Property(item => item.Caption).HasMaxLength(CatalogDomainLimits.MaxCaptionLength);
            text.Property(item => item.IsDecorative).IsRequired();
            text.Property(item => item.RequiresHumanReview).IsRequired();
            text.Property(item => item.IsAiGenerated).IsRequired();
        });

        entity.Navigation(image => image.ResponsiveVariants).Metadata.SetField("_responsiveVariants");
        entity.Navigation(image => image.TourLinks).Metadata.SetField("_tourLinks");
        entity.Navigation(image => image.AccessibilityTexts).Metadata.SetField("_accessibilityTexts");
    }
}
