using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Tour> Tours => Set<Tour>();

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

            entity.Property(tour => tour.Identifier).IsRequired().HasMaxLength(64);
            entity.Property(tour => tour.Name).IsRequired().HasMaxLength(128);
            entity.Property(tour => tour.StartDate).IsRequired();
            entity.Property(tour => tour.EndDate).IsRequired();
            entity.Property(tour => tour.Price).IsRequired();
            entity.Property(tour => tour.SingleRoomSupplementPrice).IsRequired();
            entity.Property(tour => tour.RegularBikePrice).IsRequired();
            entity.Property(tour => tour.EBikePrice).IsRequired();
            entity.Property(tour => tour.Currency).HasConversion<string>().IsRequired();
            entity.PrimitiveCollection(tour => tour.IncludedServices)
                .HasField("_includedServices")
                .IsRequired();
        });
    }
}
