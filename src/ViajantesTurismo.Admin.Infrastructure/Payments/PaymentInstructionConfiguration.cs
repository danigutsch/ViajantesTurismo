using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Maps payment instruction snapshot rows to the PaymentInstructionSnapshots table.</summary>
internal sealed class PaymentInstructionConfiguration : IEntityTypeConfiguration<PaymentInstructionRow>
{
    /// <summary>Configures the document key, protected snapshot, relationships and indexes.</summary>
    public void Configure(EntityTypeBuilder<PaymentInstructionRow> entity)
    {
        entity.ToTable("PaymentInstructionSnapshots");
        entity.HasKey(row => row.DocumentId);
        entity.Property(row => row.DocumentId).ValueGeneratedNever();
        entity.Property(row => row.ProtectedSnapshot).IsRequired();
        // Payment snapshots are purged with their unfinalized document under the retention policy.
        entity.HasOne<DocumentDraft>().WithOne().HasForeignKey<PaymentInstructionRow>(row => row.DocumentId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Booking>().WithMany().HasForeignKey(row => row.BookingId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<PaymentAccountRow>().WithMany().HasForeignKey(row => row.AccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(row => row.BookingId);
        entity.HasIndex(row => row.AccountId);
    }
}
