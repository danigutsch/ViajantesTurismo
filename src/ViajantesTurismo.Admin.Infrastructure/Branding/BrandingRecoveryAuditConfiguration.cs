using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>EF Core mapping for <see cref="BrandingRecoveryAuditRecord" /> in the branding projection schema.</summary>
internal sealed class BrandingRecoveryAuditConfiguration : IEntityTypeConfiguration<BrandingRecoveryAuditRecord>
{
    private const int MaxKindLength = 32;
    private const int MaxTextLength = 256;

    /// <summary>Maps the recovery audit table, key, required text columns and the kind and old epoch index.</summary>
    public void Configure(EntityTypeBuilder<BrandingRecoveryAuditRecord> entity)
    {
        entity.ToTable("recovery_audit", BrandingProjectionSchema.Name);
        entity.HasKey(audit => audit.Id);
        entity.Property(audit => audit.Id).ValueGeneratedNever();
        entity.Property(audit => audit.Kind).HasConversion<string>().HasMaxLength(MaxKindLength).IsRequired();
        entity.Property(audit => audit.Reason).HasMaxLength(MaxTextLength).IsRequired();
        entity.Property(audit => audit.IncidentReference).HasMaxLength(MaxTextLength).IsRequired();
        entity.Property(audit => audit.Actor).HasMaxLength(MaxTextLength).IsRequired();
        entity.HasIndex(audit => new { audit.Kind, audit.OldEpoch });
    }
}
