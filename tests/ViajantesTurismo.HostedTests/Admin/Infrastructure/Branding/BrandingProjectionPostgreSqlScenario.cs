using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Infrastructure.Branding;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Branding;

/// <summary>Runs the Admin Branding projection store against a migrated PostgreSQL database.</summary>
internal sealed class BrandingProjectionPostgreSqlScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase database;
    private readonly List<AdminWriteDbContext> contexts = [];

    private BrandingProjectionPostgreSqlScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
    }

    /// <summary>Migrates a test-owned database and returns a scenario over it.</summary>
    public static async ValueTask<BrandingProjectionPostgreSqlScenario> Create(PostgreSqlTestDatabase database, CancellationToken ct)
    {
        var scenario = new BrandingProjectionPostgreSqlScenario(database);
        await using var dbContext = scenario.CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        return scenario;
    }

    /// <summary>Creates a projection store with its own unit of work, like one worker delivery.</summary>
    public IBrandingProjectionStore CreateStore()
    {
        var dbContext = CreateDbContext();
        contexts.Add(dbContext);
        return new EfBrandingProjectionStore(dbContext);
    }

    public async Task Apply(BrandingSettingsChangedIntegrationEvent integrationEvent, CancellationToken ct) =>
        await new BrandingProjectionApplier(CreateStore(), TimeProvider.System).Apply(integrationEvent, ct);

    public BrandingProjectionRecoveryService CreateRecoveryService() => new(CreateStore(), TimeProvider.System);

    public async Task<(string Status, long AuditEntries)> ReadRecovery(CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        await using var command = dataSource.CreateCommand("""
                                                           SELECT "Status",
                                                                  (SELECT COUNT(*) FROM branding_projection.recovery_audit)
                                                           FROM branding_projection.recovery;
                                                           """);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return (reader.GetString(0), reader.GetInt64(1));
    }

    public async Task<(long Revision, string BrandName, long InboxMessages)> Read(CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        await using var command = dataSource.CreateCommand("""
                                                           SELECT "SourceRevision", "BrandName",
                                                                  (SELECT COUNT(*) FROM branding_projection.inbox_messages)
                                                           FROM branding_projection.settings;
                                                           """);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return (reader.GetInt64(0), reader.GetString(1), reader.GetInt64(2));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var dbContext in contexts)
        {
            await dbContext.DisposeAsync();
        }
    }

    private AdminWriteDbContext CreateDbContext()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(
            IntegrationEventConsumerNames.Catalog);
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;
        return new AdminWriteDbContext(options, configurations);
    }
}
