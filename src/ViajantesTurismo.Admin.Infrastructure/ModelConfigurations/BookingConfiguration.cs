using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Domain;

namespace ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;

/// <summary>EF Core mapping for the <see cref="Booking" /> entity and its owned customer and discount values.</summary>
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    /// <summary>Maps keys, required columns, enum conversions, ignored computed properties and owned types.</summary>
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
        entity.Ignore(booking => booking.FinancialHistory);
        entity.Ignore(booking => booking.HasFinancialHistory);
        entity.Ignore(booking => booking.TotalPrice);
        entity.Property(booking => booking.Notes).HasMaxLength(AdminDomainLimits.MaxBookingNotesLength);

        entity.OwnsOne(booking => booking.PrincipalCustomer, customer =>
        {
            customer.Property(c => c.CustomerId).IsRequired();
            customer.Property(c => c.BikeType).HasConversion<string>().IsRequired();
            customer.Property(c => c.BikePrice).IsRequired();
        });

        entity.OwnsOne(booking => booking.CompanionCustomer, customer =>
        {
            customer.Property(c => c.CustomerId).IsRequired();
            customer.Property(c => c.BikeType).HasConversion<string>().IsRequired();
            customer.Property(c => c.BikePrice).IsRequired();
        });

        entity.OwnsOne(booking => booking.Discount, discount =>
        {
            discount.Property(d => d.Type).HasConversion<string>().IsRequired();
            discount.Property(d => d.Amount).IsRequired();
            discount.Property(d => d.Reason).HasMaxLength(AdminDomainLimits.MaxBookingNotesLength);
        });

    }
}
