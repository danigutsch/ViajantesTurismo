using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Tours;

/// <summary>Maps the immutable audit evidence written with an accepted tour retirement.</summary>
internal sealed class TourRetirementAuditConfiguration : IEntityTypeConfiguration<TourRetirementAuditRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TourRetirementAuditRecord> entity)
    {
        entity.HasKey(record => record.RetirementId);
        entity.Property(record => record.RetirementId).ValueGeneratedNever();
        entity.Property(record => record.ActorId).HasMaxLength(TourRetirementAuditRecord.MaxActorIdLength).IsRequired();
        entity.Property(record => record.Outcome).HasConversion<string>().IsRequired();
        entity.HasIndex(record => record.TourId);
    }
}
