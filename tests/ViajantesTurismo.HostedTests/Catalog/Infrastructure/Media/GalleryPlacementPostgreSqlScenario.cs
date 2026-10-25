using Microsoft.EntityFrameworkCore;
using Npgsql;
using ViajantesTurismo.Catalog.Application.Tours;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.InfrastructureTests.Media;

internal sealed class GalleryPlacementPostgreSqlScenario : IAsyncDisposable
{
    private readonly PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease app;
    private readonly NpgsqlDataSource mutationDataSource;
    private readonly string connectionString;
    private readonly ServiceProvider serviceProvider;

    private GalleryPlacementPostgreSqlScenario(PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease app, string connectionString, ServiceProvider serviceProvider)
    {
        this.app = app;
        this.connectionString = connectionString;
        mutationDataSource = NpgsqlDataSource.Create(connectionString);
        this.serviceProvider = serviceProvider;
    }

    public static async ValueTask<GalleryPlacementPostgreSqlScenario> Create(HostedTestFixture shared, CancellationToken ct)
    {
        var app = await shared.Databases.Create(ct);
        var connectionString = app.ConnectionString;
        var services = new ServiceCollection();
        services.AddIntegrationEventOutbox<CatalogDbContext>();
        services.AddIntegrationEventInbox<CatalogDbContext>();
        services.AddIntegrationEventTransportStorage<CatalogDbContext>();
        var serviceProvider = services.BuildServiceProvider();

        return new GalleryPlacementPostgreSqlScenario(app, connectionString, serviceProvider);
    }

    public ICatalogTourMutationLock CreateMutationLock()
    {
        return new PostgreSqlCatalogTourMutationLock(mutationDataSource);
    }

    public async Task WaitForBlockedMutation(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1 FROM pg_stat_activity
                    WHERE datname = current_database()
                      AND wait_event = 'advisory'
                      AND query LIKE 'SELECT pg_advisory_xact_lock%');
                """;
            if (await command.ExecuteScalarAsync(ct) is true)
            {
                return;
            }
        }
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
        await mutationDataSource.DisposeAsync();
        await app.DisposeAsync();
    }
}
