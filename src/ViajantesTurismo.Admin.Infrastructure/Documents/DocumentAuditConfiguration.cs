using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Configures persistence for immutable document audit metadata.</summary>
internal sealed class DocumentAuditConfiguration : IEntityTypeConfiguration<DocumentAuditRecord>
{
    public void Configure(EntityTypeBuilder<DocumentAuditRecord> entity)
    {
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Id).ValueGeneratedNever();
        entity.Property(record => record.ActorId).HasMaxLength(DocumentAuditLimits.MaxActorIdLength).IsRequired();
        entity.Property(record => record.DocumentId);
        entity.Property(record => record.BookingId);
        entity.Property(record => record.DocumentRevision);
        entity.Property(record => record.Operation).HasConversion<string>().IsRequired();
        entity.Property(record => record.Outcome).HasConversion<string>().IsRequired();
        entity.Property(record => record.ReasonCode).HasConversion<string>().IsRequired();
        entity.Property(record => record.CorrelationId).HasMaxLength(DocumentAuditLimits.MaxCorrelationIdLength).IsRequired();
        entity.Property(record => record.OccurredAtUtc).IsRequired();
        entity.Property(record => record.RetentionExpiresAt).IsRequired();
        entity.HasIndex(record => record.RetentionExpiresAt)
            .HasDatabaseName("IX_DocumentAuditRecords_RetentionExpiresAt");
    }
}
