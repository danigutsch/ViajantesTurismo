using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Customer> Customers => Set<Customer>();

    public async Task SaveEntities(CancellationToken ct)
    {
        _ = await SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(tour => tour.Id);
            entity.Property(tour => tour.Id).ValueGeneratedOnAdd();

            entity.HasIndex(tour => tour.Identifier).IsUnique();
            entity.HasIndex(tour => tour.Name).IsUnique();

            entity.Property(tour => tour.Identifier).IsRequired().HasMaxLength(ContractConstants.MaxDefaultLength);
            entity.Property(tour => tour.Name).IsRequired().HasMaxLength(ContractConstants.MaxNameLength);
            entity.Property(tour => tour.StartDate).IsRequired();
            entity.Property(tour => tour.EndDate).IsRequired();
            entity.Property(tour => tour.Price).IsRequired();
            entity.Property(tour => tour.DoubleRoomSupplementPrice).IsRequired();
            entity.Property(tour => tour.RegularBikePrice).IsRequired();
            entity.Property(tour => tour.EBikePrice).IsRequired();
            entity.Property(tour => tour.Currency).HasConversion<string>().IsRequired();
            entity.Property<string[]>("_includedServices")
                .HasColumnName("IncludedServices")
                .IsRequired();

            entity.HasMany(tour => tour.Bookings)
                .WithOne()
                .HasForeignKey(booking => booking.TourId)
                .OnDelete(DeleteBehavior.Cascade)
                .Metadata.PrincipalToDependent!.SetField("_bookings");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Id).ValueGeneratedOnAdd();

            entity.OwnsOne(customer => customer.PersonalInfo, builder => builder.ToTable("CustomerPersonalInfo"));
            entity.OwnsOne(customer => customer.IdentificationInfo, builder => builder.ToTable("CustomerIdentificationInfo"));
            entity.OwnsOne(customer => customer.ContactInfo, builder => builder.ToTable("CustomerContactInfo"));
            entity.OwnsOne(customer => customer.Address, builder => builder.ToTable("CustomerAddress"));
            entity.OwnsOne(customer => customer.PhysicalInfo, builder => builder.ToTable("CustomerPhysicalInfo"));
            entity.OwnsOne(customer => customer.AccommodationPreferences, builder => builder.ToTable("CustomerAccommodationPreferences"));
            entity.OwnsOne(customer => customer.EmergencyContact, builder => builder.ToTable("CustomerEmergencyContact"));
            entity.OwnsOne(customer => customer.MedicalInfo, builder => builder.ToTable("CustomerMedicalInfo"));
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(booking => booking.Id);
            entity.Property(booking => booking.Id).ValueGeneratedOnAdd();

            entity.Property(booking => booking.TourId).IsRequired();
            entity.Property(booking => booking.BasePrice).IsRequired();
            entity.Property(booking => booking.RoomType).HasConversion<string>().IsRequired();
            entity.Property(booking => booking.RoomAdditionalCost).IsRequired();
            entity.Property(booking => booking.BookingDate).IsRequired();
            entity.Property(booking => booking.Status).HasConversion<string>().IsRequired();
            entity.Property(booking => booking.PaymentStatus).HasConversion<string>().IsRequired();
            entity.Property(booking => booking.TotalPrice).IsRequired();
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
        });
    }
}