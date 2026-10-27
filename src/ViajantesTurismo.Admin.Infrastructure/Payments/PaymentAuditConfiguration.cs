using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Maps payment audit rows to the PaymentAudits table.</summary>
internal sealed class PaymentAuditConfiguration : IEntityTypeConfiguration<PaymentAuditRow>
{
    /// <summary>Configures the key, identity sequence, length limits and resource/time index.</summary>
    public void Configure(EntityTypeBuilder<PaymentAuditRow> entity)
    {
        entity.ToTable("PaymentAudits");
        entity.HasKey(row => row.Id);
        entity.Property(row => row.Sequence).UseIdentityAlwaysColumn();
        entity.HasIndex(row => row.Sequence).IsUnique();
        entity.Property(row => row.Id).ValueGeneratedNever();
        entity.Property(row => row.ActorId).HasMaxLength(256).IsRequired();
        entity.Property(row => row.Operation).HasMaxLength(64).IsRequired();
        entity.Property(row => row.Reason).HasMaxLength(64).IsRequired();
        entity.HasIndex(row => new { row.ResourceId, row.OccurredAt });
    }
}
