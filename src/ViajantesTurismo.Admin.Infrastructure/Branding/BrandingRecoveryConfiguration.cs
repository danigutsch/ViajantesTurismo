using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>EF Core mapping for <see cref="BrandingRecoveryRecord" /> in the branding projection schema.</summary>
internal sealed class BrandingRecoveryConfiguration : IEntityTypeConfiguration<BrandingRecoveryRecord>
{
    private const int MaxStatusLength = 16;
    private const int MaxTextLength = 256;

    /// <summary>Maps the recovery table, key, status string conversion and text column lengths.</summary>
    public void Configure(EntityTypeBuilder<BrandingRecoveryRecord> entity)
    {
        entity.ToTable("recovery", BrandingProjectionSchema.Name);
        entity.HasKey(recovery => recovery.Id);
        entity.Property(recovery => recovery.Id).ValueGeneratedNever();
        entity.Property(recovery => recovery.Status).HasConversion<string>().HasMaxLength(MaxStatusLength).IsRequired();
        entity.Property(recovery => recovery.Reason).HasMaxLength(MaxTextLength);
        entity.Property(recovery => recovery.AuthorizationReason).HasMaxLength(MaxTextLength);
        entity.Property(recovery => recovery.IncidentReference).HasMaxLength(MaxTextLength);
        entity.Property(recovery => recovery.AuthorizedBy).HasMaxLength(MaxTextLength);
    }
}
