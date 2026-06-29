using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class PublicMediaImageConfiguration : IEntityTypeConfiguration<PublicMediaImage>
{
    public void Configure(EntityTypeBuilder<PublicMediaImage> entity)
    {
        entity.ToTable("PublicMediaImages");
        entity.HasKey(image => image.Id);
        entity.Property(image => image.Id).ValueGeneratedNever();

        entity.Property(image => image.SourceUri).HasConversion<string>().IsRequired();
        entity.Property(image => image.Checksum).HasMaxLength(ContractConstants.MaxChecksumLength).IsRequired();
        entity.Property(image => image.ContentType).HasMaxLength(ContractConstants.MaxContentTypeLength).IsRequired();
        entity.Property(image => image.ProcessingStatus).HasConversion<string>().IsRequired();
        entity.Property<string[]>("_tags").HasColumnName("Tags").IsRequired();
        entity.Property(image => image.AltText).HasMaxLength(ContractConstants.MaxAltTextLength).IsRequired();
        entity.Property(image => image.Caption).HasMaxLength(ContractConstants.MaxCaptionLength);
        entity.Property(image => image.Attribution).HasMaxLength(ContractConstants.MaxAttributionLength);
        entity.Property(image => image.Copyright).HasMaxLength(ContractConstants.MaxCopyrightLength);

        entity.OwnsOne(image => image.Dimensions, dimensions =>
        {
            dimensions.Property(dimension => dimension.Width).HasColumnName("Width").IsRequired();
            dimensions.Property(dimension => dimension.Height).HasColumnName("Height").IsRequired();
        });

        entity.OwnsMany(image => image.ResponsiveVariants, variant =>
        {
            variant.ToTable("PublicMediaImageResponsiveVariants");
            variant.WithOwner().HasForeignKey("PublicMediaImageId");
            variant.Property<int>("SortOrder").ValueGeneratedNever();
            variant.HasKey("PublicMediaImageId", "SortOrder");
            variant.Property(item => item.Uri).HasConversion<string>().IsRequired();
            variant.Property(item => item.Width).IsRequired();
            variant.Property(item => item.Height).IsRequired();
            variant.Property(item => item.ContentType).HasMaxLength(ContractConstants.MaxContentTypeLength).IsRequired();
            variant.Property(item => item.FileSizeBytes).IsRequired();
        });

        entity.OwnsMany(image => image.TourLinks, link =>
        {
            link.ToTable("PublicMediaImageTourLinks");
            link.WithOwner().HasForeignKey("PublicMediaImageId");
            link.HasKey("PublicMediaImageId", nameof(MediaImageTourLink.CatalogTourId));
            link.HasIndex(item => new { item.CatalogTourId, item.DisplayOrder });
            link.Property(item => item.DisplayOrder).IsRequired();
            link.Property(item => item.IsCover).IsRequired();
        });

        entity.Navigation(image => image.ResponsiveVariants).Metadata.SetField("_responsiveVariants");
        entity.Navigation(image => image.TourLinks).Metadata.SetField("_tourLinks");
    }
}
