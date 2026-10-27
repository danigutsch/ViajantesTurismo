using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using PandaBear.EventSourcing;
using PandaBear.EventSourcing.Npgsql;
using PandaBear.MalwareScanning.ClamAv;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.InfrastructureTests.Tours;

/// <summary>Catalog tour slug lock scenario backed by a leased PostgreSQL database and the Catalog event store.</summary>
internal sealed class CatalogTourSlugLockPostgreSqlScenario : IAsyncDisposable
{
    private readonly PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease app;
    private readonly ServiceProvider serviceProvider;
    private readonly IEventStore eventStore;
    private readonly AdminTourCreatedIntegrationHandler handler;

    private CatalogTourSlugLockPostgreSqlScenario(
        PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease app,
        ServiceProvider serviceProvider,
        IEventStore eventStore,
        AdminTourCreatedIntegrationHandler handler)
    {
        this.app = app;
        this.serviceProvider = serviceProvider;
        this.eventStore = eventStore;
        this.handler = handler;
    }

    /// <summary>Leases a test database, builds the Catalog infrastructure on it and initializes the event store, releasing both on failure.</summary>
    public static async ValueTask<CatalogTourSlugLockPostgreSqlScenario> Create(HostedTestFixture shared, CancellationToken ct)
    {
        var app = await shared.Databases.Create(ct);
        ServiceProvider? serviceProvider = null;
        try
        {
            var connectionString = app.ConnectionString;
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

    /// <summary>Handles the admin tour created integration event with the Catalog handler.</summary>
    public ValueTask Handle(AdminTourCreatedIntegrationEvent integrationEvent, CancellationToken ct) =>
        handler.Handle(integrationEvent, ct);

    /// <summary>Loads all events of the stream from the Catalog event store.</summary>
    public ValueTask<IReadOnlyCollection<EventEnvelope>> Load(StreamId streamId, CancellationToken ct) =>
        eventStore.Load(streamId, afterRevision: null, ct);

    /// <summary>Runs a parameterized text query so Npgsql tracing can be observed and returns the sentinel text.</summary>
    public async Task<string> ExecuteParameterizedTracingProbe(string sentinel, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sentinel);

        var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CAST(@sentinel AS text);";
        command.Parameters.AddWithValue("sentinel", sentinel);

        var result = await command.ExecuteScalarAsync(ct);
        return result as string
            ?? throw new InvalidOperationException("The Npgsql tracing probe returned no text.");
    }

    /// <summary>Disposes the service provider and the database lease.</summary>
    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        await app.DisposeAsync();
    }
}
