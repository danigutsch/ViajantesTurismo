using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Read-only DbContext optimized for query operations following CQRS pattern.
/// Configured with NoTracking behavior for improved performance.
/// </summary>
internal sealed class AdminReadDbContext(DbContextOptions<AdminReadDbContext> options) : DbContext(options)
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<DocumentDraft> DocumentDrafts => Set<DocumentDraft>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TourConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentDraftConfiguration());
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
