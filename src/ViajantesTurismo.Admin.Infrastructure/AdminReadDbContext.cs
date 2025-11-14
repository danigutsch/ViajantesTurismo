using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Read-only DbContext optimized for query operations following CQRS pattern.
/// Configured with NoTracking behavior for improved performance.
/// </summary>
internal sealed class AdminReadDbContext(DbContextOptions<AdminReadDbContext> options) : DbContext(options)
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TourConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
    }

    public override int SaveChanges()
    {
        throw new InvalidOperationException("This context is read-only. Use AdminWriteDbContext for write operations.");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This context is read-only. Use AdminWriteDbContext for write operations.");
    }
}
