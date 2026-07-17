using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.IntegrationTesting;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.InfrastructureTests.Media;

internal sealed class GalleryPlacementPostgreSqlScenario : IAsyncDisposable
{
    private const string PostgreSqlResourceName = "postgres";
    private const string DatabaseResourceName = "catalog";
    private readonly AspireTestApplication app;
    private readonly string connectionString;
    private readonly ServiceProvider serviceProvider;

    private GalleryPlacementPostgreSqlScenario(AspireTestApplication app, string connectionString, ServiceProvider serviceProvider)
    {
        this.app = app;
        this.connectionString = connectionString;
        this.serviceProvider = serviceProvider;
    }

    public static async ValueTask<GalleryPlacementPostgreSqlScenario> Create(CancellationToken ct)
    {
        var appBuilder = DistributedApplication.CreateBuilder([]);
        var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
        _ = databaseServer.AddDatabase(DatabaseResourceName);

        var app = await AspireTestApplication.Start(appBuilder, [PostgreSqlResourceName], null, ct);
        var connectionString = await app.GetConnectionString(DatabaseResourceName, ct);
        var services = new ServiceCollection();
        services.AddIntegrationEventOutbox<CatalogDbContext>();
        var serviceProvider = services.BuildServiceProvider();

        return new GalleryPlacementPostgreSqlScenario(app, connectionString, serviceProvider);
    }

    public CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var configurations = serviceProvider.GetServices<IDbContextConfiguration<CatalogDbContext>>().ToArray();
        return new CatalogDbContext(options, configurations);
    }

    public async ValueTask MigrateToLatest(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
    }

    public async ValueTask<IReadOnlyList<string>> GetGalleryIndexNames(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Database.SqlQueryRaw<string>(
                """
                SELECT indexname AS "Value"
                FROM pg_indexes
                WHERE schemaname = 'public'
                  AND tablename = 'PublicMediaImageTourLinks'
                  AND indexname LIKE 'UX_PublicMediaImageTourLinks_%'
                ORDER BY indexname
                """)
            .ToArrayAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        await app.DisposeAsync();
    }
}
