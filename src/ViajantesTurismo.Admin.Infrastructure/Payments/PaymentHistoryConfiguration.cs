using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentHistoryConfiguration : IEntityTypeConfiguration<PaymentHistoryRow>
{
    public void Configure(EntityTypeBuilder<PaymentHistoryRow> entity)
    {
        entity.ToTable("BookingPaymentHistories");
        entity.HasKey(row => row.BookingId);
        entity.Property(row => row.BookingId).ValueGeneratedNever();
        entity.Property(row => row.Version).IsConcurrencyToken();
        entity.Property(row => row.ProtectedState).IsRequired();
        entity.HasOne<Booking>().WithOne().HasForeignKey<PaymentHistoryRow>(row => row.BookingId).OnDelete(DeleteBehavior.Restrict);
    }
}
