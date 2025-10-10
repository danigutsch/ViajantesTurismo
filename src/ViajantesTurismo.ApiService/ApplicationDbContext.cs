using Microsoft.EntityFrameworkCore;

namespace ViajantesTurismo.ApiService;

internal sealed class ApplicationDbContext : DbContext
{
    public DbSet<Tour> Tours => Set<Tour>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(tour => tour.Id);
            entity.Property(tour => tour.Name).IsRequired().HasMaxLength(128);
            entity.Property(tour => tour.StartDate).IsRequired();
            entity.Property(tour => tour.EndDate).IsRequired();
        });
    }
}