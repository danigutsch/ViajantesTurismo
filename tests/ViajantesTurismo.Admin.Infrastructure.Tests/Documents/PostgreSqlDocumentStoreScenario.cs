using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SharedKernel.Domain.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.IntegrationTesting;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure.Documents;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Documents;

internal sealed class PostgreSqlDocumentStoreScenario : IAsyncDisposable
{
    private const string PostgreSqlResourceName = "postgres";
    private const string DatabaseResourceName = "admin";

    private readonly AspireTestApplication app;
    private readonly string connectionString;

    private PostgreSqlDocumentStoreScenario(AspireTestApplication app, string connectionString)
    {
        this.app = app;
        this.connectionString = connectionString;
    }

    public static async ValueTask<PostgreSqlDocumentStoreScenario> Create(CancellationToken ct)
    {
        var appBuilder = AspireTestApplication.CreateBuilder();
        var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
        _ = databaseServer.AddDatabase(DatabaseResourceName);

        var app = await AspireTestApplication.Start(appBuilder, [PostgreSqlResourceName], null, ct);
        var connectionString = await app.GetConnectionString(DatabaseResourceName, ct);

        return new PostgreSqlDocumentStoreScenario(app, connectionString);
    }

    public async ValueTask Seed(params DocumentDraft[] documents)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await SeedBookings(
            dbContext,
            documents.Select(document => document.BookingId),
            BookingStatus.Confirmed,
            TestContext.Current.CancellationToken);
        dbContext.DocumentLineages.AddRange(CreateLineages(documents));
        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask Seed(params DocumentLineage[] lineages)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        await SeedBookings(
            dbContext,
            lineages.Select(lineage => lineage.BookingId),
            BookingStatus.Confirmed,
            TestContext.Current.CancellationToken);
        dbContext.DocumentLineages.AddRange(lineages);
        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask SeedAudits(params DocumentAuditRecord[] records)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        var store = new DocumentAuditStore(dbContext);
        foreach (var record in records)
        {
            store.Add(record);
        }

        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask SeedAudit(DocumentAuditRecord record) => SeedAudits(record);

    public async ValueTask<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new DocumentStore(dbContext).PurgeExpiredDrafts(now, ct);
    }

    public async ValueTask<int> PurgeExpiredDraftsWithRetries(DateTime now, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext(enableRetryOnFailure: true);
        return await new DocumentStore(dbContext).PurgeExpiredDrafts(now, ct);
    }

    public async Task InstallLineageDeleteFailure(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE FUNCTION "FailDocumentLineageDelete"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'Injected lineage cleanup failure.';
            END;
            $$;

            CREATE TRIGGER "TR_DocumentLineages_FailDelete"
            BEFORE DELETE ON "DocumentLineages"
            FOR EACH ROW EXECUTE FUNCTION "FailDocumentLineageDelete"();
            """,
            ct);
    }

    public async ValueTask<bool> HasPurgeInvalidatedStaleLineage(
        Guid documentId,
        DateTime now,
        CancellationToken ct)
    {
        await using var staleContext = CreateDbContext();
        var staleLineage = await new DocumentStore(staleContext).GetByDocumentId(documentId, ct);

        await using (var purgeContext = CreateDbContext())
        {
            _ = await new DocumentStore(purgeContext).PurgeExpiredDrafts(now, ct);
        }

        staleLineage.ShouldNotBeNull().UpdateField(
                documentId,
                "greeting",
                "Stale update",
                now.AddMinutes(1),
                DocumentDraftInfrastructureTestData.CreateAuditContext())
            .IsSuccess.ShouldBeTrue();

        try
        {
            _ = await staleContext.SaveChangesAsync(ct);
            return false;
        }
        catch (DbUpdateConcurrencyException)
        {
            return true;
        }
    }

    public async ValueTask<DocumentDraft[]> GetDocuments(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentDrafts
            .Include(document => document.Fields)
            .OrderBy(document => document.CreatedAt)
            .ToArrayAsync(ct);
    }

    public async ValueTask<int> GetLineageCount(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentLineages.CountAsync(ct);
    }

    public async ValueTask<DocumentDraft?> GetDocumentById(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var lineage = await new DocumentStore(dbContext).GetByDocumentId(id, ct);
        return lineage?.GetRevision(id);
    }

    public async ValueTask<DocumentLineage?> GetLineageByDocumentId(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new DocumentStore(dbContext).GetByDocumentId(id, ct);
    }

    public async ValueTask<DocumentAuditMetadata?> GetAuditMetadataById(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateReadDbContext();
        return await new DocumentQueryService(dbContext).GetAuditMetadataById(id, ct);
    }

    public async ValueTask<GetDocumentDto?> GetDocumentProjectionById(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateReadDbContext();
        return await new DocumentQueryService(dbContext).GetById(id, ct);
    }

    public async ValueTask<bool> HasConcurrentLineageUpdateConflict(Guid documentId, DateTime now, CancellationToken ct)
    {
        await using var firstContext = CreateDbContext();
        await using var secondContext = CreateDbContext();
        var firstLineage = await new DocumentStore(firstContext).GetByDocumentId(documentId, ct);
        var secondLineage = await new DocumentStore(secondContext).GetByDocumentId(documentId, ct);
        var auditContext = DocumentAuditContext.Create(
            "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
            "9a3ca841b4354928861c660a6e4e1b99").Value;

        firstLineage.ShouldNotBeNull().UpdateField(
                documentId,
                "greeting",
                "First update",
                now.AddMinutes(1),
                auditContext)
            .IsSuccess.ShouldBeTrue();
        secondLineage.ShouldNotBeNull().UpdateField(
                documentId,
                "greeting",
                "Second update",
                now.AddMinutes(2),
                auditContext)
            .IsSuccess.ShouldBeTrue();
        _ = await firstContext.SaveChangesAsync(ct);

        try
        {
            _ = await secondContext.SaveChangesAsync(ct);
            return false;
        }
        catch (DbUpdateConcurrencyException)
        {
            return true;
        }
    }

    public async Task SaveDuplicateRevisions(
        DocumentDraft first,
        DocumentDraft second,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        await SeedBookings(dbContext, [first.BookingId, second.BookingId], BookingStatus.Confirmed, ct);
        dbContext.DocumentLineages.AddRange(CreateLineages([first, second]));
        await dbContext.SaveEntities(ct);
    }

    public async Task SaveDuplicateRevision(
        Guid documentLineageId,
        DocumentDraft duplicate,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        dbContext.DocumentDrafts.Add(duplicate);
        dbContext.Entry(duplicate)
            .Property(document => document.DocumentLineageId)
            .CurrentValue = documentLineageId;

        await dbContext.SaveEntities(ct);
    }

    public async Task SaveDocumentForBookingStatus(
        DocumentDraft document,
        BookingStatus? bookingStatus,
        CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        if (bookingStatus is not null)
        {
            await SeedBookings(dbContext, [document.BookingId], bookingStatus.Value, ct);
        }

        dbContext.DocumentLineages.Add(DocumentLineage.Restore([document]));
        await dbContext.SaveEntities(ct);
    }

    public async ValueTask<DocumentAuditRecord?> GetAuditRecord(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentAuditRecords.SingleOrDefaultAsync(record => record.Id == id, ct);
    }

    public async ValueTask<string[]> GetDocumentAuditColumnNames(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        return await dbContext.Database.SqlQueryRaw<string>(
                """
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'DocumentAuditRecords'
                ORDER BY column_name
                """)
            .ToArrayAsync(ct);
    }

    public async ValueTask<int> PurgeExpiredAuditRecords(DateTime now, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new DocumentAuditStore(dbContext).PurgeExpiredRecords(now, ct);
    }

    public async Task ResetMutableDataPreservingDocumentAudits(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await PostgreSqlPublicSchemaReset.Reset(connection, ["DocumentAuditRecords"], ct);
    }

    public async Task UpdateAuditActor(Guid id, string actorId, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"DocumentAuditRecords\" SET \"ActorId\" = {actorId} WHERE \"Id\" = {id}",
            ct);
    }

    public async Task DeleteAuditRecord(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"DocumentAuditRecords\" WHERE \"Id\" = {id}",
            ct);
    }

    public async Task TruncateAuditRecords(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"DocumentAuditRecords\"",
            ct);
    }

    public async ValueTask SetBrandingLogoUri(Guid documentId, string value, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var rows = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"DocumentDrafts\" SET \"BrandingLogoUri\" = {value} WHERE \"Id\" = {documentId}",
            ct);

        rows.ShouldBe(1);
    }

    public async ValueTask<bool> HasRetentionIndex(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var count = await dbContext.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM pg_indexes
                WHERE schemaname = 'public'
                  AND tablename = 'DocumentDrafts'
                  AND indexname = 'IX_DocumentDrafts_RetentionExpiresAt_Unfinalized'
                  AND indexdef LIKE '%WHERE ("FinalizedAt" IS NULL)%'
                """)
            .SingleAsync(ct);

        return count == 1;
    }

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    private AdminWriteDbContext CreateDbContext(bool enableRetryOnFailure = false)
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions =>
            {
                if (enableRetryOnFailure)
                {
                    npgsqlOptions.EnableRetryOnFailure();
                }
            })
            .Options;

        return new AdminWriteDbContext(options, configurations);
    }

    private AdminReadDbContext CreateReadDbContext()
    {
        var options = new DbContextOptionsBuilder<AdminReadDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AdminReadDbContext(options);
    }

    private static async Task SeedBookings(
        AdminWriteDbContext dbContext,
        IEnumerable<Guid> bookingIds,
        BookingStatus bookingStatus,
        CancellationToken ct)
    {
        foreach (var bookingId in bookingIds.Distinct())
        {
            var tourId = Guid.CreateVersion7();
            var customerId = Guid.CreateVersion7();
            var bookingDate = DateTime.UtcNow;
            var tourIdentifier = $"document-fixture-{tourId:N}";

            _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Tours" (
                    "Id", "Identifier", "Name", "StartDate", "EndDate", "Price",
                    "DoubleRoomSupplementPrice", "RegularBikePrice", "EBikePrice", "Currency",
                    "MinCustomers", "MaxCustomers", "IncludedServices")
                VALUES (
                    {tourId}, {tourIdentifier}, {tourIdentifier}, {bookingDate.AddDays(30)},
                    {bookingDate.AddDays(37)}, 0, 0, 0, 0, 'UsDollar', 1, 1, ARRAY['Document']);
                """,
                ct);

            _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Booking" (
                    "Id", "TourId", "BasePrice", "RoomType", "RoomAdditionalCost",
                    "PrincipalCustomer_CustomerId", "PrincipalCustomer_BikeType", "PrincipalCustomer_BikePrice",
                    "Discount_Type", "Discount_Amount", "BookingDate", "Status")
                VALUES (
                    {bookingId}, {tourId}, 0, 'DoubleOccupancy', 0, {customerId}, 'Regular', 0, 'None', 0,
                    {bookingDate}, {bookingStatus.ToString()});
                """,
                ct);
        }
    }

    private static DocumentLineage[] CreateLineages(IEnumerable<DocumentDraft> documents) =>
        documents
            .GroupBy(document => document.DocumentLineageId)
            .Select(group => DocumentLineage.Restore(group))
            .ToArray();
}
