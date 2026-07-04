using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class AdminWriteDbContext(
    DbContextOptions<AdminWriteDbContext> options,
    IEnumerable<IAdminWriteDbContextModule>? modules = null)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Customer> Customers => Set<Customer>();

    internal DbSet<IntegrationEventOutboxMessage> IntegrationEventOutbox => Set<IntegrationEventOutboxMessage>();

    public async Task SaveEntities(CancellationToken ct)
    {
        _ = await SaveChangesAsync(ct);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (modules is not null)
        {
            foreach (var module in modules)
            {
                module.Configure(optionsBuilder);
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TourConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        if (modules is not null)
        {
            foreach (var module in modules)
            {
                module.Configure(modelBuilder);
            }
        }
    }
}
