using Microsoft.EntityFrameworkCore;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Infrastructure.Documents;

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
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        dbContext.DocumentDrafts.AddRange(documents);
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

    public async ValueTask DisposeAsync() => await app.DisposeAsync();

    private AdminWriteDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AdminWriteDbContext(options);
    }
}
