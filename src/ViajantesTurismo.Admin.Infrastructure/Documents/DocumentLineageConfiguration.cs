using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Configures document lineage aggregate persistence.</summary>
internal sealed class DocumentLineageConfiguration : IEntityTypeConfiguration<DocumentLineage>
{
    public void Configure(EntityTypeBuilder<DocumentLineage> entity)
    {
        entity.HasKey(lineage => lineage.Id);
        entity.Property(lineage => lineage.Id).ValueGeneratedNever();
        entity.Property(lineage => lineage.BookingId).IsRequired();
        entity.Property(lineage => lineage.Type).HasConversion<string>().IsRequired();
        entity.Property(lineage => lineage.Audience).HasConversion<string>().IsRequired();
        entity.Property(lineage => lineage.HighestRevision).IsRequired();
        entity.Property(lineage => lineage.HighestFinalizedRevision).IsRequired();
        entity.Property(lineage => lineage.Version).IsConcurrencyToken().IsRequired();
        entity.HasIndex(lineage => new { lineage.BookingId, lineage.Type })
            .IsUnique()
            .HasDatabaseName(DocumentDraftSchema.LineageUniqueIndex);
        entity.HasMany(lineage => lineage.Revisions)
            .WithOne()
            .HasForeignKey(document => document.DocumentLineageId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
        entity.Navigation(lineage => lineage.Revisions).Metadata.SetField("_revisions");
        entity.Navigation(lineage => lineage.Revisions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
