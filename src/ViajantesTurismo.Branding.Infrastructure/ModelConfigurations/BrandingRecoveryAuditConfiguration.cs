using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Branding.Infrastructure.ModelConfigurations;

/// <summary>EF Core mapping for <see cref="BrandingRecoveryAuditRecord"/>.</summary>
internal sealed class BrandingRecoveryAuditConfiguration : IEntityTypeConfiguration<BrandingRecoveryAuditRecord>
{
    private const int MaxKindLength = 32;
    private const int MaxTextLength = 256;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BrandingRecoveryAuditRecord> entity)
    {
        entity.ToTable("BrandingRecoveryAudits", BrandingDbContext.SchemaName);
        entity.HasKey(audit => audit.Id);
        entity.Property(audit => audit.Id).ValueGeneratedNever();
        entity.Property(audit => audit.Kind).HasMaxLength(MaxKindLength).IsRequired();
        entity.Property(audit => audit.Reason).HasMaxLength(MaxTextLength).IsRequired();
        entity.Property(audit => audit.IncidentReference).HasMaxLength(MaxTextLength).IsRequired();
        entity.Property(audit => audit.Actor).HasMaxLength(MaxTextLength).IsRequired();
    }
}
