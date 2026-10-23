using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentInstructionConfiguration : IEntityTypeConfiguration<PaymentInstructionRow>
{
    public void Configure(EntityTypeBuilder<PaymentInstructionRow> entity)
    {
        entity.ToTable("PaymentInstructionSnapshots");
        entity.HasKey(row => row.DocumentId);
        entity.Property(row => row.DocumentId).ValueGeneratedNever();
        entity.Property(row => row.ProtectedSnapshot).IsRequired();
        // Retain the existing provisional unfinalized-document purge behavior.
        entity.HasOne<DocumentDraft>().WithOne().HasForeignKey<PaymentInstructionRow>(row => row.DocumentId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Booking>().WithMany().HasForeignKey(row => row.BookingId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<PaymentAccountRow>().WithMany().HasForeignKey(row => row.AccountId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(row => row.BookingId);
        entity.HasIndex(row => row.AccountId);
    }
}
