using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.PublicTheme;

namespace ViajantesTurismo.Catalog.Infrastructure.ModelConfigurations;

internal sealed class PublicThemeSettingsConfiguration : IEntityTypeConfiguration<PublicThemeSettings>
{
    public void Configure(EntityTypeBuilder<PublicThemeSettings> entity)
    {
        entity.ToTable("PublicThemeSettings");

        entity.HasKey(theme => theme.Id);
        entity.Property(theme => theme.Id).ValueGeneratedNever();
        entity.Property(theme => theme.PrimaryColor).HasMaxLength(ContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(theme => theme.AccentColor).HasMaxLength(ContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(theme => theme.BackgroundColor).HasMaxLength(ContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(theme => theme.TextColor).HasMaxLength(ContractConstants.MaxCssColorLength).IsRequired();
        entity.Property(theme => theme.HeadingFontFamily).HasMaxLength(ContractConstants.MaxCssFontFamilyLength).IsRequired();
        entity.Property(theme => theme.BodyFontFamily).HasMaxLength(ContractConstants.MaxCssFontFamilyLength).IsRequired();
    }
}
