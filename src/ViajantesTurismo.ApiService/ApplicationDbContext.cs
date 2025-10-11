using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain;

namespace ViajantesTurismo.ApiService;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Tour> Tours => Set<Tour>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(tour => tour.Id);

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
            entity.Property(tour => tour.IncludedServices).IsRequired();
        });
    }
}
