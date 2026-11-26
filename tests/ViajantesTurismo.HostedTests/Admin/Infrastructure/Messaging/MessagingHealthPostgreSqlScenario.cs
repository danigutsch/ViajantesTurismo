using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.IntegrationTesting;
using SharedKernel.Mediator;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Messaging;

/// <summary>Exercises the application's registered messaging query and EF models in process.</summary>
internal sealed class MessagingHealthPostgreSqlScenario(ServiceProvider provider, string connectionString) : IAsyncDisposable
{
    /// <summary>Registers the Admin services and migrates a test-owned database through the write model.</summary>
    internal static async Task<MessagingHealthPostgreSqlScenario> Create(string connectionString, CancellationToken ct)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration[$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString;
        builder.Services.AddSingleton(TimeProvider.System);
        builder.AddInfrastructure(addRuntimeBackgroundServices: false);

        var services = builder.Services.BuildServiceProvider();
        try
        {
            await services.MigrateAdminDatabase(ct);
            return new MessagingHealthPostgreSqlScenario(services, connectionString);
        }
        catch
        {
            await services.DisposeAsync();
            throw;
        }
    }

    /// <summary>Queues the real tour creation event with the relay disabled on the writing host.</summary>
    internal async Task<Result<Guid>> CreateTour(CreateTourCommand command, CancellationToken ct)
    {
        using var writer = BuildAdminHost(addRuntimeBackgroundServices: false);
        await using var scope = writer.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(command, ct);
    }

    /// <summary>Starts the real Admin relay and observes exact retained publication from the separate read host.</summary>
    internal async Task<GetOutboxDeliveryPageDto> RunRelayUntilPublished(OutboxDeliveryRequest request, CancellationToken ct)
    {
        using var relay = BuildAdminHost(addRuntimeBackgroundServices: true);
        var retainedRequest = request with
        {
            IncludePublished = true,
        };
        try
        {
            await relay.StartAsync(ct);
            return await Eventually.Until(
                async ct =>
                {
                    var page = await ReadOutboxDelivery(retainedRequest, ct);
                    return page.Rows.Count == 1 && page.Rows[0].PublishedAt is not null ? page : null;
                },
                TimeSpan.FromSeconds(20),
                ct);
        }
        finally
        {
            await relay.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>Reads the current aggregate through the registered application query.</summary>
    internal async Task<GetMessagingHealthDto> Read(CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<IMessagingHealthQuery>();
        return await query.ReadMessagingHealth(ct);
    }

    /// <summary>Reads the aggregate through the read context registered by the application.</summary>
    internal async Task<IntegrationEventOutboxHealth> ReadPendingOutbox(CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminReadDbContext>();
        return await IntegrationEventMessagingHealth.ReadOutbox(context, TimeProvider.System.GetUtcNow(), ct);
    }

    /// <summary>Reads an inspection page through the application's registered Catalog query.</summary>
    internal async Task<GetCatalogDeliveryPageDto> ReadCatalogDelivery(CatalogDeliveryRequest request, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<ICatalogDeliveryQuery>();
        return await query.ReadCatalogDelivery(request, ct);
    }

    /// <summary>Reads an inspection page through the application's registered outbox query.</summary>
    internal async Task<GetOutboxDeliveryPageDto> ReadOutboxDelivery(OutboxDeliveryRequest request, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var query = scope.ServiceProvider.GetRequiredService<IOutboxDeliveryQuery>();
        return await query.ReadOutboxDelivery(request, ct);
    }

    /// <summary>Inspects migration ownership and query tracking on the registered context models.</summary>
    internal async Task<(bool? ReadExcludesTransport, bool? WriteExcludesTransport,
        bool? ReadExcludesOutbox, bool? WriteExcludesOutbox, QueryTrackingBehavior ReadTracking)> InspectModels()
    {
        await using var scope = provider.CreateAsyncScope();
        var read = scope.ServiceProvider.GetRequiredService<AdminReadDbContext>();
        var write = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();

        _ = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        return (MigrationExclusion(read, "transport_messages"), MigrationExclusion(write, "transport_messages"),
            MigrationExclusion(read, "outbox_messages"), MigrationExclusion(write, "outbox_messages"),
            read.ChangeTracker.QueryTrackingBehavior);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await provider.DisposeAsync();

    private IHost BuildAdminHost(bool addRuntimeBackgroundServices)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration[$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString;
        builder.AddApplication();
        builder.Services.AddMediator();
        builder.AddInfrastructure(addRuntimeBackgroundServices);
        return builder.Build();
    }

    private static bool? MigrationExclusion(DbContext context, string tableName)
    {
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.GetEntityTypes().SingleOrDefault(candidate =>
            candidate.GetTableName() == tableName && candidate.GetSchema() == "messaging");
        return entity?.IsTableExcludedFromMigrations();
    }
}
