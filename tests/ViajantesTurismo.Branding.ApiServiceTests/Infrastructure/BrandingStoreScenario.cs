using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PandaBear.Messaging;
using PandaBear.Messaging.IntegrationEvents;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

/// <summary>
/// Runs the Branding settings store against an in-memory database with the real Branding outbox.
/// </summary>
internal sealed class BrandingStoreScenario : IDisposable
{
    private readonly ServiceProvider provider;
    private readonly List<IServiceScope> scopes = [];

    private BrandingStoreScenario(ServiceProvider provider)
    {
        this.provider = provider;
    }

    public static BrandingStoreScenario Create()
    {
        var databaseName = Guid.CreateVersion7().ToString("N");
        var root = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<BrandingDbContext>(options => options
            .UseInMemoryDatabase(databaseName, root)
            .EnableServiceProviderCaching(false));
        services.AddKeyedSingleton<IIntegrationEventSerializer, BrandingIntegrationEventSerializer>(typeof(BrandingDbContext));
        services.ConfigureIntegrationEventStorage<BrandingDbContext>(
            BrandingInfrastructureDependencyInjection.ConfigureBrandingIntegrationEventStorage);
        services.AddIntegrationEventOutbox<BrandingDbContext>();
        services.AddScoped<EfBrandingSettingsStore>();

        return new BrandingStoreScenario(services.BuildServiceProvider());
    }

    /// <summary>Creates a store with its own unit of work, like one request.</summary>
    public EfBrandingSettingsStore CreateStore()
    {
        var scope = provider.CreateScope();
        scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<EfBrandingSettingsStore>();
    }

    /// <summary>Seeds a settings row as it existed before revisions were introduced.</summary>
    public async Task SeedUnrevisionedSettings(BrandingSettings settings, CancellationToken ct)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BrandingDbContext>();
        dbContext.BrandingSettings.Add(BrandingSettingsRecord.FromUnrevisionedSettings(settings));
        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>Seeds a settings row whose persisted values no longer validate.</summary>
    public async Task SeedInvalidUnrevisionedSettings(CancellationToken ct)
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BrandingDbContext>();
        var record = BrandingSettingsRecord.FromUnrevisionedSettings(BrandingDefaults.CreateSettings());
        dbContext.BrandingSettings.Add(record);
        dbContext.Entry(record).Property(nameof(BrandingSettingsRecord.PrimaryColor)).CurrentValue = "not-a-color";
        await dbContext.SaveChangesAsync(ct);
    }

    public BrandingStoreSnapshot ReadSnapshot()
    {
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BrandingDbContext>();
        var current = dbContext.BrandingSettings.AsNoTracking().SingleOrDefault();
        var revisions = dbContext.BrandingSettingsRevisions.AsNoTracking()
            .OrderBy(revision => revision.SourceRevision)
            .ToArray();
        var outboxType = dbContext.Model.GetEntityTypes()
            .Single(entity => typeof(IIntegrationEventOutboxMessage).IsAssignableFrom(entity.ClrType))
            .ClrType;
        var outbox = ((IQueryable<object>)typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                .MakeGenericMethod(outboxType)
                .Invoke(dbContext, null)!)
            .AsEnumerable()
            .Cast<EventEnvelope>()
            .ToArray();

        return new BrandingStoreSnapshot(current, revisions, outbox);
    }

    public void Dispose()
    {
        foreach (var scope in scopes)
        {
            scope.Dispose();
        }

        provider.Dispose();
    }
}
