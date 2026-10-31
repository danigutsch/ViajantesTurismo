using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PandaBear.Branding;

namespace ViajantesTurismo.Branding.Infrastructure.ModelConfigurations;

/// <summary>EF Core mapping for <see cref="BrandingSettingsRecord"/>.</summary>
internal sealed class BrandingSettingsConfiguration : IEntityTypeConfiguration<BrandingSettingsRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BrandingSettingsRecord> entity)
    {
        entity.ToTable("BrandingSettings", BrandingDbContext.SchemaName);

        entity.HasKey(settings => settings.Id);
        entity.Property(settings => settings.Id).ValueGeneratedNever();
        entity.Property(settings => settings.BrandName).HasMaxLength(BrandingContractConstants.MaxBrandNameLength).IsRequired();
        entity.Property(settings => settings.PrimaryColor).HasMaxLength(BrandingContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(settings => settings.AccentColor).HasMaxLength(BrandingContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(settings => settings.BackgroundColor).HasMaxLength(BrandingContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(settings => settings.TextColor).HasMaxLength(BrandingContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(settings => settings.HeadingFontFamily).HasMaxLength(BrandingContractConstants.MaxFontFamilyLength).IsRequired();
        entity.Property(settings => settings.BodyFontFamily).HasMaxLength(BrandingContractConstants.MaxFontFamilyLength).IsRequired();
        entity.Property(settings => settings.LogoUri).HasMaxLength(BrandingContractConstants.MaxLogoUriLength);
        entity.Property(settings => settings.LogoSha256).HasMaxLength(BrandingLogoPath.HashLength).IsFixedLength();
        entity.HasOne<BrandingLogoRecord>().WithMany().HasForeignKey(settings => settings.LogoSha256).OnDelete(DeleteBehavior.Restrict);
        entity.Property(settings => settings.SourceEpoch).IsRequired();
        entity.Property(settings => settings.SourceRevision).IsRequired().IsConcurrencyToken();
    }
}
