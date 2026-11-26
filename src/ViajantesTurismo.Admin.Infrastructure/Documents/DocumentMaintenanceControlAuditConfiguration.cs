using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Maps the private, append-only audit for maintenance schedule controls.</summary>
internal sealed class DocumentMaintenanceControlAuditConfiguration : IEntityTypeConfiguration<DocumentMaintenanceControlAudit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DocumentMaintenanceControlAudit> entity)
    {
        entity.ToTable("control_audit", DocumentMaintenanceSchedule.DurableSchema);
        entity.HasKey(audit => audit.RequestId);
        entity.Property(audit => audit.RequestId).ValueGeneratedNever();
        entity.Property(audit => audit.Job).HasConversion<string>().IsRequired();
        entity.Property(audit => audit.Action).HasConversion<string>().IsRequired();
        entity.Property(audit => audit.ActorId).HasMaxLength(DocumentAuditLimits.MaxActorIdLength).IsRequired();
        entity.Property(audit => audit.Reason).IsRequired();
        entity.Property(audit => audit.OccurredAtUtc).IsRequired();
    }
}
