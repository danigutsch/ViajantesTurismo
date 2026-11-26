using ViajantesTurismo.Admin.Infrastructure.Customers;
using ViajantesTurismo.Admin.Infrastructure.Tours;
using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Read-only DbContext optimized for query operations following CQRS pattern.
/// Configured with NoTracking behavior for improved performance.
/// </summary>
internal sealed class AdminReadDbContext(
    DbContextOptions<AdminReadDbContext> options,
    IEnumerable<IDbContextConfiguration<AdminReadDbContext>>? configurations = null) : DbContext(options)
{
    /// <summary>Tours queried without tracking.</summary>
    public DbSet<Tour> Tours => Set<Tour>();
    /// <summary>Customers queried without tracking.</summary>
    public DbSet<Customer> Customers => Set<Customer>();
    /// <summary>Document drafts queried without tracking.</summary>
    public DbSet<DocumentDraft> DocumentDrafts => Set<DocumentDraft>();
    /// <summary>Metadata evidence from the document audit retention job.</summary>
    public DbSet<DocumentAuditPurgeRecord> DocumentAuditPurgeRecords => Set<DocumentAuditPurgeRecord>();
    /// <summary>Metadata evidence from the draft and idempotency retention jobs.</summary>
    public DbSet<DocumentRetentionPurgeRecord> DocumentRetentionPurgeRecords => Set<DocumentRetentionPurgeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.ApplyConfiguration(new PaymentAccountConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentInstructionConfiguration());
        modelBuilder.ApplyConfiguration(new TourConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentDraftConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentArtifactBodyConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentAuditPurgeConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentRetentionPurgeConfiguration());

        foreach (var configuration in configurations ?? [])
        {
            configuration.ConfigureModel(modelBuilder);
        }
    }

    public override int SaveChanges()
    {
        throw new InvalidOperationException("This context is read-only. Use AdminWriteDbContext for write operations.");
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct)
    {
        throw new InvalidOperationException("This context is read-only. Use AdminWriteDbContext for write operations.");
    }
}
