using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Domain.PublicTheme;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class PublicThemeSettingsConfiguration : IEntityTypeConfiguration<PublicThemeSettings>
{
    public void Configure(EntityTypeBuilder<PublicThemeSettings> entity)
    {
        entity.ToTable("PublicThemeSettings");

        entity.HasKey(theme => theme.Id);
        entity.Property(theme => theme.Id).ValueGeneratedNever();
        entity.Property(theme => theme.PrimaryColor).HasMaxLength(CatalogDomainLimits.MaxCssColorLength).IsRequired();
        entity.Property(theme => theme.AccentColor).HasMaxLength(CatalogDomainLimits.MaxCssColorLength).IsRequired();
        entity.Property(theme => theme.BackgroundColor).HasMaxLength(CatalogDomainLimits.MaxCssColorLength).IsRequired();
        entity.Property(theme => theme.TextColor).HasMaxLength(CatalogDomainLimits.MaxCssColorLength).IsRequired();
        entity.Property(theme => theme.HeadingFontFamily).HasMaxLength(CatalogDomainLimits.MaxCssFontFamilyLength).IsRequired();
        entity.Property(theme => theme.BodyFontFamily).HasMaxLength(CatalogDomainLimits.MaxCssFontFamilyLength).IsRequired();
    }
}
