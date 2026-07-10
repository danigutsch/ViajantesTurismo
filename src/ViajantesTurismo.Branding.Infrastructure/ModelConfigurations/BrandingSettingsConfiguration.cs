using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Branding;

namespace ViajantesTurismo.Branding.Infrastructure.ModelConfigurations;

internal sealed class BrandingSettingsConfiguration : IEntityTypeConfiguration<BrandingSettingsRecord>
{
    public void Configure(EntityTypeBuilder<BrandingSettingsRecord> entity)
    {
        entity.ToTable("BrandingSettings");

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
    }
}
