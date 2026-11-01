using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using PandaBear.MalwareScanning.ClamAv;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Branding;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>
/// Composes one infrastructure module, forces its Npgsql data sources into existence without opening a connection,
/// and exposes the pool names those data sources report.
/// </summary>
internal sealed class DataSourceNamingScenario : IDisposable
{
    private readonly ServiceProvider provider;

    private DataSourceNamingScenario(ServiceProvider provider)
    {
        this.provider = provider;
    }

    /// <summary>Composes the Admin API infrastructure and touches its write and tour capacity lock data sources.</summary>
    public static DataSourceNamingScenario ForAdmin()
    {
        var builder = CreateBuilder();
        builder.AddInfrastructure();
        var scenario = new DataSourceNamingScenario(builder.Services.BuildServiceProvider());
        scenario.TouchContext<AdminWriteDbContext>();
        scenario.TouchContext<AdminReadDbContext>();
        scenario.TouchKeyedDataSource(ResourceNames.AdminDatabase);

        return scenario;
    }

    /// <summary>Composes the Admin integration-event worker and touches its Catalog transport data source.</summary>
    public static DataSourceNamingScenario ForAdminWorker()
    {
        var builder = CreateBuilder();
        builder.AddAdminIntegrationEventWorkerInfrastructure();
        var scenario = new DataSourceNamingScenario(builder.Services.BuildServiceProvider());
        scenario.TouchContext<AdminBrandingTransportDbContext>();

        return scenario;
    }

    /// <summary>Composes Catalog infrastructure and touches its default and lock data sources.</summary>
    public static DataSourceNamingScenario ForCatalog()
    {
        var builder = CreateBuilder();
        builder.AddCatalogInfrastructure();
        var scenario = new DataSourceNamingScenario(builder.Services.BuildServiceProvider());
        scenario.TouchDefaultDataSource();
        scenario.TouchKeyedDataSource(ResourceNames.CatalogDatabase);

        return scenario;
    }

    /// <summary>Composes the Catalog integration-event transport and touches its Admin database data source.</summary>
    public static DataSourceNamingScenario ForCatalogTransport()
    {
        var builder = CreateBuilder();
        builder.AddCatalogHostedIntegrationEventTransport();
        var scenario = new DataSourceNamingScenario(builder.Services.BuildServiceProvider());
        scenario.TouchContext<CatalogIntegrationTransportDbContext>();

        return scenario;
    }

    /// <summary>Composes Branding infrastructure and touches its data source.</summary>
    public static DataSourceNamingScenario ForBranding()
    {
        var builder = CreateBuilder();
        builder.AddBrandingInfrastructure();
        var scenario = new DataSourceNamingScenario(builder.Services.BuildServiceProvider());
        scenario.TouchContext<BrandingDbContext>();

        return scenario;
    }

    /// <summary>Disposes the service provider.</summary>
    public void Dispose()
    {
        provider.Dispose();
    }

    private static HostApplicationBuilder CreateBuilder()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        var connectionString = $"Host=localhost;Database=naming-{Guid.NewGuid():N};Username=test;Password=test";
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString,
            [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = connectionString,
            [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
        });
        builder.Services.AddOpenTelemetry().WithTracing(static _ => { });

        return builder;
    }

    private void TouchContext<TContext>()
        where TContext : DbContext
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        using var connection = context.Database.GetDbConnection();
    }

    private void TouchDefaultDataSource()
    {
        _ = provider.GetRequiredService<NpgsqlDataSource>();
    }

    private void TouchKeyedDataSource(string key)
    {
        _ = provider.GetRequiredKeyedService<NpgsqlDataSource>(key);
    }
}
