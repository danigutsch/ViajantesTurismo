using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Branding;
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

        return new AdminIntegrationEventWorkerScenario(services.BuildServiceProvider());
    }

    public string[] HostedServiceNames() =>
        provider.GetServices<IHostedService>()
            .Select(service => service.GetType())
            .Select(type => type.IsGenericType
                ? $"{type.Name}<{string.Join(",", type.GenericTypeArguments.Select(argument => argument.Name))}>"
                : type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

    public (string? Schema, string? Table) TableOf<TContext>(string clrTypeName)
        where TContext : DbContext
    {
        using var scope = provider.CreateScope();
        var entityType = scope.ServiceProvider.GetRequiredService<TContext>().Model.GetEntityTypes()
            .ShouldHaveSingleItem(entity => entity.ClrType.Name == clrTypeName);
        return (entityType.GetSchema(), entityType.GetTableName());
    }

    /// <summary>Applies one event in its own unit of work, like one transport delivery.</summary>
    public async Task Apply(ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding.BrandingSettingsChangedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BrandingProjectionApplier>().Apply(integrationEvent, ct);
    }

    public async Task<(BrandingProjectionState? Current, int InboxCount)> ReadProjection(CancellationToken ct)
    {
        await using var scope = provider.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IBrandingProjectionStore>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var inboxCount = await dbContext.Set<BrandingInboxRecord>().CountAsync(ct);
        return (await store.GetCurrent(ct), inboxCount);
    }

    public void Dispose() => provider.Dispose();
}
