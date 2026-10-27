extern alias AdminWorker;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Infrastructure;
using PandaBear.Messaging;
using ViajantesTurismo.Admin.Infrastructure.Branding;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Composes the Admin integration-event worker, or runs its projection store on an in-memory database.</summary>
internal sealed class AdminIntegrationEventWorkerScenario : IDisposable
{
    private readonly ServiceProvider provider;

    private AdminIntegrationEventWorkerScenario(ServiceProvider provider)
    {
        this.provider = provider;
    }

    /// <summary>Composes the Admin integration-event worker infrastructure for production with placeholder connection strings.</summary>
    public static AdminIntegrationEventWorkerScenario ComposeWorker()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "Host=localhost;Database=admin;Username=test;Password=test",
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=catalog;Username=test;Password=test"
        });
        builder.AddAdminIntegrationEventWorkerInfrastructure();

        return new AdminIntegrationEventWorkerScenario(builder.Services.BuildServiceProvider());
    }

    /// <summary>Creates a scenario whose branding projection store runs on a fresh in-memory database with worker event delivery registered.</summary>
    public static AdminIntegrationEventWorkerScenario InMemoryProjection()
    {
        var databaseName = Guid.CreateVersion7().ToString("N");
        var root = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<AdminWriteDbContext>(options => options
            .UseInMemoryDatabase(databaseName, root)
            .EnableServiceProviderCaching(false));
        services.AddScoped<IBrandingProjectionStore, EfBrandingProjectionStore>();
        services.AddAdminBrandingProjection();
        AdminWorker::ViajantesTurismo.Admin.IntegrationEventWorker.AdminWorkerIntegrationEvents.AddAdminWorkerIntegrationEventDelivery(services);

        return new AdminIntegrationEventWorkerScenario(services.BuildServiceProvider());
    }

    /// <summary>Returns the sorted display names of the registered hosted services, including generic type arguments.</summary>
    public string[] HostedServiceNames() =>
        provider.GetServices<IHostedService>()
            .Select(service => service.GetType())
            .Select(type => type.IsGenericType
                ? $"{type.Name}<{string.Join(",", type.GenericTypeArguments.Select(argument => argument.Name))}>"
                : type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

    /// <summary>Returns the schema and table name mapped for the entity whose CLR type has the given name.</summary>
    public (string? Schema, string? Table) TableOf<TContext>(string clrTypeName)
        where TContext : DbContext
    {
        using var scope = provider.CreateScope();
        var entityType = scope.ServiceProvider.GetRequiredService<TContext>().Model.GetEntityTypes()
            .ShouldHaveSingleItem(entity => entity.ClrType.Name == clrTypeName);
        return (entityType.GetSchema(), entityType.GetTableName());
    }

    /// <summary>Applies one event in its own unit of work, like one transport delivery.</summary>
    public async Task Apply(BrandingSettingsChangedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BrandingProjectionApplier>().Apply(integrationEvent, ct);
    }

    /// <summary>Delivers one event as a transport envelope through the worker's generated publisher.</summary>
    public async Task Deliver(BrandingSettingsChangedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        var envelope = new EventEnvelope(
            "cloudevents",
            "1.0",
            integrationEvent.EventId.ToString("N"),
            new Uri("urn:viajantes:branding"),
            BrandingSettingsChangedIntegrationEvent.EventType,
            BrandingSettingsChangedIntegrationEvent.EventVersion,
            integrationEvent.OccurredAt,
            subject: null,
            "application/json",
            dataSchema: null,
            JsonSerializer.Serialize(
                integrationEvent,
                BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent),
            EventPayloadEncoding.Json,
            extensionAttributesJson: null);
        await provider.GetRequiredService<IEventEnvelopePublisher>().Publish(envelope, ct);
    }

    /// <summary>Reads the current branding projection state and the number of inbox records.</summary>
    public async Task<(BrandingProjectionState? Current, int InboxCount)> ReadProjection(CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IBrandingProjectionStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var inboxCount = await dbContext.Set<BrandingInboxRecord>().CountAsync(ct);
        return (await store.GetCurrent(ct), inboxCount);
    }

    /// <summary>Disposes the service provider.</summary>
    public void Dispose() => provider.Dispose();
}
