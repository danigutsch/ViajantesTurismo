using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> entity)
    {
        entity.HasKey(booking => booking.Id);
        entity.Property(booking => booking.Id).ValueGeneratedNever();

        entity.Property(booking => booking.TourId).IsRequired();
        entity.Property(booking => booking.BasePrice).IsRequired();
        entity.Property(booking => booking.RoomType).HasConversion<string>().IsRequired();
        entity.Property(booking => booking.RoomAdditionalCost).IsRequired();
        entity.Property(booking => booking.BookingDate).IsRequired();
        entity.Property(booking => booking.Status).HasConversion<string>().IsRequired();
        entity.Ignore(booking => booking.PaymentStatus);
        entity.Ignore(booking => booking.TotalPrice);
        entity.Property(booking => booking.Notes).HasMaxLength(ContractConstants.MaxBookingNotesLength);

        entity.OwnsOne(booking => booking.PrincipalCustomer, customer =>
        {
            customer.Property(c => c.CustomerId).IsRequired();
            customer.Property(c => c.BikeType).HasConversion<string>().IsRequired();
            customer.Property(c => c.BikePrice).IsRequired();

            customer.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        entity.OwnsOne(booking => booking.CompanionCustomer, customer =>
        {
            customer.Property(c => c.CustomerId).IsRequired();
            customer.Property(c => c.BikeType).HasConversion<string>().IsRequired();
            customer.Property(c => c.BikePrice).IsRequired();

            customer.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        entity.OwnsOne(booking => booking.Discount, discount =>
        {
            discount.Property(d => d.Type).HasConversion<string>().IsRequired();
            discount.Property(d => d.Amount).IsRequired();
            discount.Property(d => d.Reason).HasMaxLength(ContractConstants.MaxBookingNotesLength);
        });

        entity.HasMany(booking => booking.Payments)
            .WithOne()
            .HasForeignKey(payment => payment.BookingId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetField("_payments");
    }
}
