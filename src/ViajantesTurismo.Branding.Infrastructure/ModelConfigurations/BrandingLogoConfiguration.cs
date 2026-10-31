using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Branding.Infrastructure.ModelConfigurations;

/// <summary>EF Core mapping for <see cref="BrandingLogoRecord"/>.</summary>
internal sealed class BrandingLogoConfiguration : IEntityTypeConfiguration<BrandingLogoRecord>
{
    private const int MaxContentTypeLength = 32;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BrandingLogoRecord> entity)
    {
        entity.ToTable("BrandingLogos", BrandingDbContext.SchemaName);

        entity.HasKey(logo => logo.Sha256);
        entity.Property(logo => logo.Sha256).HasMaxLength(BrandingLogoPath.HashLength).IsFixedLength().ValueGeneratedNever();
        entity.Property(logo => logo.ContentType).HasMaxLength(MaxContentTypeLength).IsRequired();
        entity.Property(logo => logo.Content).IsRequired();
        entity.Property(logo => logo.StoredAt).IsRequired();
    }
}
