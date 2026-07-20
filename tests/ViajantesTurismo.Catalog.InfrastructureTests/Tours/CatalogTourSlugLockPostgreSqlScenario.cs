using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SharedKernel.EventSourcing;
using SharedKernel.EventSourcing.Npgsql;
using SharedKernel.IntegrationTesting;
using SharedKernel.MalwareScanning.ClamAv;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.InfrastructureTests.Tours;

internal sealed class CatalogTourSlugLockPostgreSqlScenario : IAsyncDisposable
{
    private const string PostgreSqlResourceName = "postgres";
    private const string DatabaseResourceName = "catalog";
    private readonly AspireTestApplication app;
    private readonly ServiceProvider serviceProvider;
    private readonly IEventStore eventStore;
    private readonly AdminTourCreatedIntegrationHandler handler;

    private CatalogTourSlugLockPostgreSqlScenario(
        AspireTestApplication app,
        ServiceProvider serviceProvider,
        IEventStore eventStore,
        AdminTourCreatedIntegrationHandler handler)
    {
        this.app = app;
        this.serviceProvider = serviceProvider;
        this.eventStore = eventStore;
        this.handler = handler;
    }

    public static async ValueTask<CatalogTourSlugLockPostgreSqlScenario> Create(CancellationToken ct)
    {
        var appBuilder = DistributedApplication.CreateBuilder([]);
        var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
        _ = databaseServer.AddDatabase(DatabaseResourceName);

        var app = await AspireTestApplication.Start(appBuilder, [PostgreSqlResourceName], null, ct);
        ServiceProvider? serviceProvider = null;
        try
        {
            var connectionString = await app.GetConnectionString(DatabaseResourceName, ct);
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                MaxPoolSize = 1
            };
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                EnvironmentName = Environments.Development
            });
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = connectionStringBuilder.ConnectionString,
                [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
            });
            builder.AddCatalogInfrastructure(addOutboxRelay: false);
            serviceProvider = builder.Services.BuildServiceProvider();

            var eventStore = serviceProvider.GetRequiredService<IEventStore>();
            var postgreSqlEventStore = eventStore as PostgreSqlEventStore
                ?? throw new InvalidOperationException("Catalog event store is not PostgreSQL-backed.");
            await postgreSqlEventStore.Initialize(ct);
            var handler = new AdminTourCreatedIntegrationHandler(
                eventStore,
                serviceProvider.GetRequiredService<ICatalogTourSlugLock>());

            return new CatalogTourSlugLockPostgreSqlScenario(app, serviceProvider, eventStore, handler);
        }
        catch
        {
            if (serviceProvider is not null)
            {
                await serviceProvider.DisposeAsync();
            }

            await app.DisposeAsync();
            throw;
        }
    }

    public ValueTask Handle(AdminTourCreatedIntegrationEvent integrationEvent, CancellationToken ct) =>
        handler.Handle(integrationEvent, ct);

    public ValueTask<IReadOnlyCollection<EventEnvelope>> Load(StreamId streamId, CancellationToken ct) =>
        eventStore.Load(streamId, afterRevision: null, ct);

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        await app.DisposeAsync();
    }
}
