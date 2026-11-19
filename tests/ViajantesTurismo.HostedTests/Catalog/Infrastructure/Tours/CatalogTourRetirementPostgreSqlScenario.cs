using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.EventSourcing;
using PandaBear.EventSourcing.Npgsql;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Runs Catalog retirement with the production event store, read models and outbox.</summary>
internal sealed class CatalogTourRetirementPostgreSqlScenario(ServiceProvider services) : IAsyncDisposable
{
    /// <summary>Connects the Catalog infrastructure to the fixture database and applies migrations.</summary>
    internal static async ValueTask<CatalogTourRetirementPostgreSqlScenario> Create(string connectionString, CancellationToken ct)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = connectionString,
            [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
        });
        builder.AddCatalogInfrastructure(addOutboxRelay: false);
        var services = builder.Services.BuildServiceProvider();
        try
        {
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync(ct);
            var eventStore = services.GetRequiredService<IEventStore>().ShouldBeOfType<PostgreSqlEventStore>();
            await eventStore.Initialize(ct);
            return new CatalogTourRetirementPostgreSqlScenario(services);
        }
        catch
        {
            await services.DisposeAsync();
            throw;
        }
    }

    /// <summary>Consumes a retirement in its own Catalog scope.</summary>
    internal async Task Retire(AdminTourRetiredIntegrationEvent retirement, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<AdminTourRetiredIntegrationEvent>>()
            .Handle(retirement, ct);
    }

    /// <summary>Reads the number of Catalog withdrawal rows and the stable completion event identifier.</summary>
    internal async Task<(int MarkerCount, string[] EventIds)> ReadCompletion(Guid adminTourId, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var markerCount = await context.CatalogTourRetirements.CountAsync(row => row.AdminTourId == adminTourId, ct);
        var eventType = CatalogTourRetirementAppliedIntegrationEvent.EventType;
        var eventIds = await context.Database.SqlQuery<string>(
            $"""
            SELECT "EventId" AS "Value" FROM "messaging"."outbox_messages"
            WHERE "EventType" = {eventType}
            """).ToArrayAsync(ct);
        return (markerCount, eventIds);
    }

    /// <summary>Causes outbox inserts to fail at database commit until the fault is removed.</summary>
    internal async Task InjectOutboxFailure(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        _ = await context.Database.ExecuteSqlRawAsync(
            """
            CREATE FUNCTION fail_catalog_retirement_outbox() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                RAISE EXCEPTION 'Injected Catalog retirement outbox failure';
            END;
            $$;
            CREATE TRIGGER fail_retirement_outbox BEFORE INSERT ON "messaging"."outbox_messages"
            FOR EACH ROW EXECUTE FUNCTION fail_catalog_retirement_outbox();
            """, ct);
    }

    /// <summary>Allows the next outbox insert to succeed after an injected failure.</summary>
    internal async Task RemoveOutboxFailure(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        _ = await context.Database.ExecuteSqlRawAsync(
            """DROP TRIGGER fail_retirement_outbox ON "messaging"."outbox_messages";""", ct);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await services.DisposeAsync();
    }
}
