using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;

internal sealed class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> entity)
    {
        entity.HasKey(tour => tour.Id);
        entity.Property(tour => tour.Id).ValueGeneratedNever();

        entity.HasIndex(tour => tour.Identifier).IsUnique();
        entity.HasIndex(tour => tour.Name).IsUnique();

        entity.Property(tour => tour.Identifier).IsRequired().HasMaxLength(ContractConstants.MaxDefaultLength);
        entity.Property(tour => tour.Name).IsRequired().HasMaxLength(ContractConstants.MaxNameLength);

        entity.OwnsOne(tour => tour.Schedule, schedule =>
        {
            schedule.Property(s => s.StartDate).HasColumnName("StartDate").IsRequired();
            schedule.Property(s => s.EndDate).HasColumnName("EndDate").IsRequired();
        });

        entity.OwnsOne(tour => tour.Pricing, pricing =>
        {
            pricing.Property(p => p.BasePrice).HasColumnName("Price").IsRequired();
            pricing.Property(p => p.DoubleRoomSupplementPrice).HasColumnName("DoubleRoomSupplementPrice")
                .IsRequired();
            pricing.Property(p => p.RegularBikePrice).HasColumnName("RegularBikePrice").IsRequired();
            pricing.Property(p => p.EBikePrice).HasColumnName("EBikePrice").IsRequired();
            pricing.Property(p => p.Currency).HasColumnName("Currency").HasConversion<string>().IsRequired();
        });

        entity.OwnsOne(tour => tour.Capacity, capacity =>
        {
            capacity.Property(c => c.MinCustomers).HasColumnName("MinCustomers").IsRequired();
            capacity.Property(c => c.MaxCustomers).HasColumnName("MaxCustomers").IsRequired();
        });

        entity.Property<string[]>("_includedServices")
            .HasColumnName("IncludedServices")
            .IsRequired();

        entity.HasMany(tour => tour.Bookings)
            .WithOne()
            .HasForeignKey(booking => booking.TourId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetField("_bookings");
    }
}
