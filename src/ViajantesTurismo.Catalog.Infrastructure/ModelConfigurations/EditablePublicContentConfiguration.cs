using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class EditablePublicContentConfiguration : IEntityTypeConfiguration<EditablePublicContent>
{
    public void Configure(EntityTypeBuilder<EditablePublicContent> entity)
    {
        entity.ToTable("PublicContent");

        entity.HasKey(content => content.Id);
        entity.Property(content => content.Id).ValueGeneratedNever();

        entity.HasIndex(content => content.Key).IsUnique();
        entity.Property(content => content.Key).IsRequired().HasMaxLength(ContractConstants.MaxDefaultLength);
        entity.Property(content => content.SourceLanguage).HasConversion<string>().IsRequired();
        entity.Property(content => content.PublicationState).HasConversion<string>().IsRequired();

        entity.OwnsMany(content => content.Variants, variant =>
        {
            variant.ToTable("PublicContentVariants");
            variant.WithOwner().HasForeignKey("PublicContentId");
            variant.Property(v => v.Language).HasConversion<string>().IsRequired();
            variant.Property(v => v.Title).HasMaxLength(ContractConstants.MaxNameLength).IsRequired();
            variant.Property(v => v.Body).HasMaxLength(ContractConstants.MaxBodyLength).IsRequired();
            variant.Property(v => v.SeoTitle).HasMaxLength(ContractConstants.MaxNameLength);
            variant.Property(v => v.MetaDescription).HasMaxLength(ContractConstants.MaxCaptionLength);
            variant.Property(v => v.ShareSummary).HasMaxLength(ContractConstants.MaxCaptionLength);
            variant.Property(v => v.RequiresHumanReview).IsRequired();
            variant.HasKey("PublicContentId", nameof(PublicContentVariant.Language));
        });

        entity.Navigation(content => content.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
