using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using SharedKernel.MalwareScanning.ClamAv;
using SharedKernel.ServiceDefaults;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Observability;

/// <summary>Exercises native metrics through a registered application or initialization pool.</summary>
internal static class PostgreSqlMetricScenario
{
    /// <summary>Queries a real PostgreSQL database and collects the metrics exported by the host composition.</summary>
    public static async Task<IReadOnlyList<(string Instrument, IReadOnlyList<KeyValuePair<string, object?>> Tags)>> Query(
        string connectionString,
        string poolName,
        bool initialization,
        CancellationToken ct)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.Configuration[$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString;
        builder.Configuration[$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = connectionString;
        builder.Configuration[ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "scanner.example";
        builder.Configuration["Catalog:MediaObjectStorage:SeaweedFs:Endpoint"] = "https://object-storage.example";
        builder.Configuration["Catalog:MediaObjectStorage:SeaweedFs:Bucket"] = "media";
        builder.Configuration["Catalog:MediaObjectStorage:SeaweedFs:AccessKey"] = "test-only";
        builder.Configuration["Catalog:MediaObjectStorage:SeaweedFs:SecretKey"] = "test-only";
        builder.AddApplicationDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();
        using var exporter = new PostgreSqlMetricExporter();
        builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
            metrics.AddReader(new PeriodicExportingMetricReader(exporter)));

        switch (poolName, initialization)
        {
            case ("admin", false):
                builder.AddInfrastructure(addRuntimeBackgroundServices: false);
                break;
            case ("admin", true):
                builder.AddAdminDatabaseInitialization();
                break;
            case ("catalog", false):
                builder.AddCatalogInfrastructure(addOutboxRelay: false);
                break;
            case ("catalog", true):
                builder.AddCatalogDatabaseInitialization();
                break;
            case ("management-security" or "management-ticket-cache", false):
                builder.Services.AddManagementSecurityPersistence(connectionString);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(poolName));
        }

        using var host = builder.Build();
        var meter = host.Services.GetRequiredService<MeterProvider>();
        await using var scope = host.Services.CreateAsyncScope();
        DbContext context = poolName switch
        {
            "admin" => scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>(),
            "catalog" => scope.ServiceProvider.GetRequiredService<CatalogDbContext>(),
            _ => scope.ServiceProvider.GetRequiredService<ManagementSecurityDbContext>(),
        };
        if (poolName == "management-ticket-cache")
        {
            await context.Database.MigrateAsync(ct);
            var cache = host.Services.GetRequiredService<IDistributedCache>();
            (await cache.GetAsync("metric_private_cache_key", ct)).ShouldBeNull();
        }
        else
        {
            const string probe = "metric_private_parameter";
            var result = await context.Database.SqlQuery<string>($"""
                SELECT {probe} AS "Value" /* metric_private_object */
                """).SingleAsync(ct);
            result.ShouldBe(probe);
        }
        meter.ForceFlush().ShouldBeTrue();
        return exporter.Points;
    }
}
