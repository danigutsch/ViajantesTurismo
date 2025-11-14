using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> entity)
    {
        entity.HasKey(payment => payment.Id);
        entity.Property(payment => payment.Id).ValueGeneratedNever();

        entity.Property(payment => payment.BookingId).IsRequired();
        entity.Property(payment => payment.Amount).IsRequired();
        entity.Property(payment => payment.PaymentDate).IsRequired();
        entity.Property(payment => payment.Method).HasConversion<string>().IsRequired();
        entity.Property(payment => payment.ReferenceNumber).HasMaxLength(ContractConstants.MaxReferenceNumberLength);
        entity.Property(payment => payment.Notes).HasMaxLength(ContractConstants.MaxPaymentNotesLength);
        entity.Property(payment => payment.RecordedAt).IsRequired();

        entity.HasIndex(payment => payment.BookingId);
    }
}
