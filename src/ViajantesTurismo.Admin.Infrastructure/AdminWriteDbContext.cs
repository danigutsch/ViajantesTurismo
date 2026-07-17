using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class AdminWriteDbContext(
    DbContextOptions<AdminWriteDbContext> options,
    IEnumerable<IDbContextConfiguration<AdminWriteDbContext>>? configurations = null)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<DocumentDraft> DocumentDrafts => Set<DocumentDraft>();
    public DbSet<DocumentAuditRecord> DocumentAuditRecords => Set<DocumentAuditRecord>();

    public async Task SaveEntities(CancellationToken ct)
    {
        _ = await SaveChangesAsync(ct);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        if (configurations is not null)
        {
            foreach (var configuration in configurations)
            {
                configuration.ConfigureConventions(configurationBuilder);
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
        modelBuilder.ApplyConfiguration(new DocumentDraftConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentAuditConfiguration());
        if (configurations is not null)
        {
            foreach (var configuration in configurations)
            {
                configuration.ConfigureModel(modelBuilder);
            }
        }
    }
}
