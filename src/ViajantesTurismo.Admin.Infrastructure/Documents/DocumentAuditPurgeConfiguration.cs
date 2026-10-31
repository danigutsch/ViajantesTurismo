using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Configures persistence for metadata-only document audit purge evidence.</summary>
internal sealed class DocumentAuditPurgeConfiguration : IEntityTypeConfiguration<DocumentAuditPurgeRecord>
{
    /// <summary>Maps purge evidence, its enum conversion and length limits.</summary>
    public void Configure(EntityTypeBuilder<DocumentAuditPurgeRecord> entity)
    {
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Id).ValueGeneratedNever();
        entity.Property(record => record.ActorId).HasMaxLength(DocumentAuditLimits.MaxActorIdLength).IsRequired();
        entity.Property(record => record.CorrelationId).HasMaxLength(DocumentAuditLimits.MaxCorrelationIdLength).IsRequired();
        entity.Property(record => record.PurgedCount).IsRequired();
        entity.Property(record => record.HeldCount).HasDefaultValue(0).IsRequired();
        entity.Property(record => record.UndecidableCount).HasDefaultValue(0).IsRequired();
        entity.Property(record => record.RetentionCutoffUtc).IsRequired();
        entity.Property(record => record.Outcome).HasConversion<string>().IsRequired();
        entity.Property(record => record.OccurredAtUtc).IsRequired();
    }
}
