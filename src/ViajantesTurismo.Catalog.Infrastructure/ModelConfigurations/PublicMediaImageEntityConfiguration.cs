using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class PublicMediaImageEntityConfiguration : IEntityTypeConfiguration<PublicMediaImageEntity>
{
    public void Configure(EntityTypeBuilder<PublicMediaImageEntity> entity)
    {
        entity.ToTable("PublicMediaImages");
        entity.HasKey(image => image.Id);
        entity.Property(image => image.Id).ValueGeneratedNever();

        entity.Property(image => image.SourceUri).IsRequired();
        entity.Property(image => image.Checksum).HasMaxLength(ContractConstants.MaxChecksumLength).IsRequired();
        entity.Property(image => image.ContentType).HasMaxLength(ContractConstants.MaxContentTypeLength).IsRequired();
        entity.Property(image => image.ProcessingStatus).HasConversion<string>().IsRequired();
        entity.Property(image => image.Tags).IsRequired();
        entity.Property(image => image.AltText).HasMaxLength(ContractConstants.MaxAltTextLength).IsRequired();
        entity.Property(image => image.Caption).HasMaxLength(ContractConstants.MaxCaptionLength);
        entity.Property(image => image.Attribution).HasMaxLength(ContractConstants.MaxAttributionLength);
        entity.Property(image => image.Copyright).HasMaxLength(ContractConstants.MaxCopyrightLength);

        entity.HasMany(image => image.ResponsiveVariants)
            .WithOne()
            .HasForeignKey(variant => variant.PublicMediaImageId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetField("_responsiveVariants");

        entity.HasMany(image => image.TourLinks)
            .WithOne()
            .HasForeignKey(link => link.PublicMediaImageId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetField("_tourLinks");
    }
}
