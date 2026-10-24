using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

internal sealed class BrandingProjectionConfiguration : IEntityTypeConfiguration<BrandingProjectionRecord>
{
    private const int MaxNameLength = 120;
    private const int MaxColorLength = 7;
    private const int MaxLogoUriLength = 2048;

    public void Configure(EntityTypeBuilder<BrandingProjectionRecord> entity)
    {
        entity.ToTable("settings", BrandingProjectionSchema.Name);
        entity.HasKey(projection => projection.Id);
        entity.Property(projection => projection.Id).ValueGeneratedNever();
        entity.Property(projection => projection.SourceEpoch).IsRequired();
        entity.Property(projection => projection.SourceRevision).IsRequired().IsConcurrencyToken();
        entity.Property(projection => projection.BrandName).HasMaxLength(MaxNameLength).IsRequired();
        entity.Property(projection => projection.PrimaryColor).HasMaxLength(MaxColorLength).IsRequired();
        entity.Property(projection => projection.AccentColor).HasMaxLength(MaxColorLength).IsRequired();
        entity.Property(projection => projection.BackgroundColor).HasMaxLength(MaxColorLength).IsRequired();
        entity.Property(projection => projection.TextColor).HasMaxLength(MaxColorLength).IsRequired();
        entity.Property(projection => projection.HeadingFontFamily).HasMaxLength(MaxNameLength).IsRequired();
        entity.Property(projection => projection.BodyFontFamily).HasMaxLength(MaxNameLength).IsRequired();
        entity.Property(projection => projection.LogoUri).HasMaxLength(MaxLogoUriLength);
    }
}
