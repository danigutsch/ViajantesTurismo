using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PandaBear.Branding;

namespace ViajantesTurismo.Branding.Infrastructure.ModelConfigurations;

/// <summary>EF Core mapping for <see cref="BrandingSettingsRevisionRecord"/>.</summary>
internal sealed class BrandingSettingsRevisionConfiguration : IEntityTypeConfiguration<BrandingSettingsRevisionRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BrandingSettingsRevisionRecord> entity)
    {
        entity.ToTable("BrandingSettingsRevisions", BrandingDbContext.SchemaName);

        entity.HasKey(revision => revision.Id);
        entity.Property(revision => revision.Id).ValueGeneratedNever();
        entity.HasIndex(revision => new { revision.SourceEpoch, revision.SourceRevision }).IsUnique();
        entity.Property(revision => revision.BrandName).HasMaxLength(BrandingContractConstants.MaxBrandNameLength).IsRequired();
        entity.Property(revision => revision.PrimaryColor).HasMaxLength(BrandingContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(revision => revision.AccentColor).HasMaxLength(BrandingContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(revision => revision.BackgroundColor).HasMaxLength(BrandingContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(revision => revision.TextColor).HasMaxLength(BrandingContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(revision => revision.HeadingFontFamily).HasMaxLength(BrandingContractConstants.MaxFontFamilyLength).IsRequired();
        entity.Property(revision => revision.BodyFontFamily).HasMaxLength(BrandingContractConstants.MaxFontFamilyLength).IsRequired();
        entity.Property(revision => revision.LogoUri).HasMaxLength(BrandingContractConstants.MaxLogoUriLength);
    }
}
