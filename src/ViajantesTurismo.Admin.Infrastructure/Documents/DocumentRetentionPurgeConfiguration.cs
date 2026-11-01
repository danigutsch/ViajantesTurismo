using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Configures persistence for counts-only draft and idempotency purge evidence.</summary>
internal sealed class DocumentRetentionPurgeConfiguration : IEntityTypeConfiguration<DocumentRetentionPurgeRecord>
{
    /// <summary>Maps purge evidence and its enum conversions.</summary>
    public void Configure(EntityTypeBuilder<DocumentRetentionPurgeRecord> entity)
    {
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Id).ValueGeneratedNever();
        entity.Property(record => record.Kind).HasConversion<string>().IsRequired();
        entity.Property(record => record.PurgedCount).IsRequired();
        entity.Property(record => record.HeldCount).IsRequired();
        entity.Property(record => record.Outcome).HasConversion<string>().IsRequired();
        entity.Property(record => record.OccurredAtUtc).IsRequired();
    }
}
