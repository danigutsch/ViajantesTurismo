using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class AdminWriteDbContext(
    DbContextOptions<AdminWriteDbContext> options,
    IEnumerable<ISaveChangesInterceptor>? saveChangesInterceptors = null)
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

        if (saveChangesInterceptors is not null)
        {
            optionsBuilder.AddInterceptors(saveChangesInterceptors);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TourConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new IntegrationEventOutboxMessageConfiguration());
    }
}
