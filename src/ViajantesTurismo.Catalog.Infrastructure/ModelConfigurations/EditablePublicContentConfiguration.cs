using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

/// <summary>EF Core mapping for editable public content and its language variants.</summary>
internal sealed class EditablePublicContentConfiguration : IEntityTypeConfiguration<EditablePublicContent>
{
    /// <summary>Maps the content table, unique key, enum conversions and owned variants.</summary>
    public void Configure(EntityTypeBuilder<EditablePublicContent> entity)
    {
        entity.ToTable("PublicContent");

        entity.HasKey(content => content.Id);
        entity.Property(content => content.Id).ValueGeneratedNever();

        entity.HasIndex(content => content.Key).IsUnique();
        entity.Property(content => content.Key).IsRequired().HasMaxLength(CatalogDomainLimits.MaxDefaultLength);
        entity.Property(content => content.SourceLanguage).HasConversion<string>().IsRequired();
        entity.Property(content => content.PublicationState).HasConversion<string>().IsRequired();

        entity.OwnsMany(content => content.Variants, variant =>
        {
            variant.ToTable("PublicContentVariants");
            variant.WithOwner().HasForeignKey("PublicContentId");
            variant.Property(v => v.Language).HasConversion<string>().IsRequired();
            variant.Property(v => v.Title).HasMaxLength(CatalogDomainLimits.MaxNameLength).IsRequired();
            variant.Property(v => v.Body).HasMaxLength(CatalogDomainLimits.MaxBodyLength).IsRequired();
            variant.Property(v => v.SeoTitle).HasMaxLength(CatalogDomainLimits.MaxNameLength);
            variant.Property(v => v.MetaDescription).HasMaxLength(CatalogDomainLimits.MaxCaptionLength);
            variant.Property(v => v.ShareSummary).HasMaxLength(CatalogDomainLimits.MaxCaptionLength);
            variant.Property(v => v.RequiresHumanReview).IsRequired();
            variant.HasKey("PublicContentId", nameof(PublicContentVariant.Language));
        });

        entity.Navigation(content => content.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
