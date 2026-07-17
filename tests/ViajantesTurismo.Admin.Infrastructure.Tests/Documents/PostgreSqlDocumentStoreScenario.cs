using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.DomainEvents.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.IntegrationTesting;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Documents;
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
        var appBuilder = DistributedApplication.CreateBuilder([]);
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
        dbContext.DocumentDrafts.AddRange(documents);
        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask SeedAudit(DocumentAuditRecord record)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(TestContext.Current.CancellationToken);
        new DocumentAuditStore(dbContext).Add(record);
        _ = await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask<int> PurgeExpiredDrafts(DateTime now, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new DocumentStore(dbContext).PurgeExpiredDrafts(now, ct);
    }

    public async ValueTask<DocumentDraft[]> GetDocuments(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentDrafts
            .Include(document => document.Fields)
            .OrderBy(document => document.CreatedAt)
            .ToArrayAsync(ct);
    }

    public async ValueTask<DocumentDraft?> GetDocumentById(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new DocumentStore(dbContext).GetById(id, ct);
    }

    public async ValueTask<DocumentAuditMetadata?> GetAuditMetadataById(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateReadDbContext();
        return await new DocumentQueryService(dbContext).GetAuditMetadataById(id, ct);
    }

    public async ValueTask<bool> HasConcurrentDocumentUpdateConflict(Guid id, DateTime now, CancellationToken ct)
    {
        await using var firstContext = CreateDbContext();
        await using var secondContext = CreateDbContext();
        var firstDocument = await firstContext.DocumentDrafts
            .Include(document => document.Fields)
            .SingleAsync(document => document.Id == id, ct);
        var secondDocument = await secondContext.DocumentDrafts
            .Include(document => document.Fields)
            .SingleAsync(document => document.Id == id, ct);

        firstDocument.UpdateField("greeting", "First update", now.AddMinutes(1)).IsSuccess.ShouldBeTrue();
        secondDocument.UpdateField("greeting", "Second update", now.AddMinutes(2)).IsSuccess.ShouldBeTrue();
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

    public async ValueTask<DocumentAuditRecord?> GetAuditRecord(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.DocumentAuditRecords.SingleOrDefaultAsync(record => record.Id == id, ct);
    }

    public async ValueTask<int> PurgeExpiredAuditRecords(DateTime now, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new DocumentAuditStore(dbContext).PurgeExpiredRecords(now, ct);
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

    private AdminWriteDbContext CreateDbContext()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(connectionString)
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
}
