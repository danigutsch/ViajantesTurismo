using Microsoft.EntityFrameworkCore;
using Npgsql;
using ViajantesTurismo.Catalog.Application.Tours;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PandaBear.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using PandaBear.ObjectStorage;
using PandaBear.Results;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Media;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Media;

/// <summary>Gallery placement scenario backed by a leased PostgreSQL test database.</summary>
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

    /// <summary>Leases a test database and builds the scenario with the integration event services registered.</summary>
    public static async ValueTask<GalleryPlacementPostgreSqlScenario> Create(HostedTestFixture shared, CancellationToken ct)
    {
        var app = await shared.Databases.Create(ct);
        var connectionString = app.ConnectionString;
        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));
        services.AddCatalogApplication();
        services.AddIntegrationEventOutbox<CatalogDbContext>();
        services.AddIntegrationEventInbox<CatalogDbContext>();
        services.AddIntegrationEventTransportStorage<CatalogDbContext>();
        var serviceProvider = services.BuildServiceProvider();

        return new GalleryPlacementPostgreSqlScenario(app, connectionString, serviceProvider);
    }

    /// <summary>Creates a PostgreSQL tour mutation lock on the scenario data source.</summary>
    public ICatalogTourMutationLock CreateMutationLock()
    {
        return new PostgreSqlCatalogTourMutationLock(mutationDataSource);
    }

    /// <summary>Polls until a session is waiting on a PostgreSQL advisory transaction lock.</summary>
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

    /// <summary>Creates a Catalog database context on the scenario database.</summary>
    public CatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var configurations = serviceProvider.GetServices<IDbContextConfiguration<CatalogDbContext>>().ToArray();
        return new CatalogDbContext(options, configurations);
    }

    /// <summary>Runs the upload intake in a fresh Catalog context with the durable outbox.</summary>
    public async ValueTask<Result<MediaImageUploadIntakeResult>> AcceptUpload(
        MediaImageUploadIntakeRequest request, IMediaUploadScanner scanner, IObjectStore objects, CancellationToken ct,
        MediaUploadValidationOptions? validationOptions = null)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var options = validationOptions ?? new MediaUploadValidationOptions();
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(options),
            scanner,
            objects,
            new EfPublicMediaImageStore(context),
            scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>(),
            Options.Create(options),
            NullLogger<MediaImageUploadIntake>.Instance,
            TimeProvider.System);

        return await intake.Accept(request, ct);
    }

    /// <summary>Reads the number of durable image and integration outbox records.</summary>
    public async ValueTask<(int Images, long Events)> ReadUploadCounts(CancellationToken ct)
    {
        await using var context = CreateDbContext();
        return (
            await context.PublicMediaImages.CountAsync(ct),
            await context.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM messaging.outbox_messages").SingleAsync(ct));
    }

    /// <summary>Applies all pending Catalog migrations to the scenario database.</summary>
    public async ValueTask MigrateToLatest(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
    }

    /// <summary>Gets the names of the unique gallery indexes on the public media image tour links table, ordered by name.</summary>
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

    /// <summary>Disposes the service provider, the data source and the database lease.</summary>
    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        await mutationDataSource.DisposeAsync();
        await app.DisposeAsync();
    }
}
