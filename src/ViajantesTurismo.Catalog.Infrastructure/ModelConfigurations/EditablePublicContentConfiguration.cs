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

        ConfigureVariant(entity.OwnsOne(content => content.EnUs), "EnUs");
        ConfigureVariant(entity.OwnsOne(content => content.PtBr), "PtBr");
    }

    private static void ConfigureVariant(
        OwnedNavigationBuilder<EditablePublicContent, PublicContentVariant> variant,
        string prefix)
    {
        variant.Property(v => v.Language).HasColumnName($"{prefix}Language").HasConversion<string>().IsRequired();
        variant.Property(v => v.Title).HasColumnName($"{prefix}Title").HasMaxLength(ContractConstants.MaxNameLength).IsRequired();
        variant.Property(v => v.Body).HasColumnName($"{prefix}Body").HasMaxLength(ContractConstants.MaxBodyLength).IsRequired();
        variant.Property(v => v.SeoTitle).HasColumnName($"{prefix}SeoTitle").HasMaxLength(ContractConstants.MaxNameLength);
        variant.Property(v => v.MetaDescription).HasColumnName($"{prefix}MetaDescription").HasMaxLength(ContractConstants.MaxCaptionLength);
        variant.Property(v => v.ShareSummary).HasColumnName($"{prefix}ShareSummary").HasMaxLength(ContractConstants.MaxCaptionLength);
        variant.Property(v => v.RequiresHumanReview).HasColumnName($"{prefix}RequiresHumanReview").IsRequired();
    }
}
