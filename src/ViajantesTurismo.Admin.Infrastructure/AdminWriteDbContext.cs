using ViajantesTurismo.Admin.Infrastructure.Customers;
using ViajantesTurismo.Admin.Infrastructure.Tours;
using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using PandaBear.BuildingBlocks;
using PandaBear.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore.Npgsql;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Customers;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Branding;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Tracking DbContext for write operations; also the unit of work that commits aggregate changes.
/// </summary>
/// <param name="options">EF Core options for this context.</param>
/// <param name="configurations">Optional extra model and convention configurations contributed by SharedKernel modules.</param>
internal sealed class AdminWriteDbContext(
    DbContextOptions<AdminWriteDbContext> options,
    IEnumerable<IDbContextConfiguration<AdminWriteDbContext>>? configurations = null)
    : DbContext(options), IUnitOfWork
{
    private static readonly NpgsqlUniqueViolationParser UniqueViolationParser = new();

    /// <summary>Tour aggregates.</summary>
    public DbSet<Tour> Tours => Set<Tour>();
    /// <summary>Tour retirement audit evidence.</summary>
    public DbSet<TourRetirementAuditRecord> TourRetirementAuditRecords => Set<TourRetirementAuditRecord>();
    /// <summary>Customer aggregates.</summary>
    public DbSet<Customer> Customers => Set<Customer>();
    /// <summary>Document lineages.</summary>
    public DbSet<DocumentLineage> DocumentLineages => Set<DocumentLineage>();
    /// <summary>Document drafts.</summary>
    public DbSet<DocumentDraft> DocumentDrafts => Set<DocumentDraft>();
    /// <summary>Document audit records.</summary>
    public DbSet<DocumentAuditRecord> DocumentAuditRecords => Set<DocumentAuditRecord>();
    /// <summary>Document audit purge evidence.</summary>
    public DbSet<DocumentAuditPurgeRecord> DocumentAuditPurgeRecords => Set<DocumentAuditPurgeRecord>();
    /// <summary>Draft and idempotency purge evidence.</summary>
    public DbSet<DocumentRetentionPurgeRecord> DocumentRetentionPurgeRecords => Set<DocumentRetentionPurgeRecord>();

    /// <summary>Saves tracked changes, translating unique-constraint and revision conflicts into domain conflict exceptions.</summary>
    public async Task SaveEntities(CancellationToken ct)
    {
        try
        {
            await DocumentAuditLineage.StampAddedRecords(this, ct);
            _ = await this.SaveChangesTranslatingUniqueViolations(UniqueViolationParser, ct);
        }
        catch (UniqueConstraintViolationException exception) when (exception.Rule == CustomerSchema.EmailUniqueRule)
        {
            throw new CustomerEmailConflictException(exception);
        }
        catch (UniqueConstraintViolationException exception) when (exception.Rule == DocumentDraftSchema.RevisionUniqueRule)
        {
            throw new DocumentRevisionConflictException(exception);
        }
        catch (DbUpdateException exception) when (DocumentDraftSchema.IsBookingEligibilityConflict(exception))
        {
            throw new DocumentBookingEligibilityConflictException(exception);
        }
        catch (DbUpdateException exception) when (DocumentDraftSchema.IsDuplicateArtifactConflict(exception))
        {
            throw new DocumentRevisionConflictException(exception);
        }
        catch (Npgsql.PostgresException exception) when (DocumentDraftSchema.IsActiveFinalizedLineageConflict(exception))
        {
            throw new DocumentRevisionConflictException(exception);
        }
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

        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfiguration(new PaymentAccountConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentInstructionConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentAuditConfiguration());
        modelBuilder.ApplyConfiguration(new TourConfiguration());
        modelBuilder.ApplyConfiguration(new TourRetirementAuditConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentLineageConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentDraftConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentArtifactBodyConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentAuditConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentAuditPurgeConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentRetentionPurgeConfiguration());
        modelBuilder.ApplyConfiguration(new BrandingProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new BrandingInboxConfiguration());
        modelBuilder.ApplyConfiguration(new BrandingRecoveryConfiguration());
        modelBuilder.ApplyConfiguration(new BrandingRecoveryAuditConfiguration());
        if (configurations is not null)
        {
            foreach (var configuration in configurations)
            {
                configuration.ConfigureModel(modelBuilder);
            }
        }
    }
}
