using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Observability.Npgsql;
using ViajantesTurismo.DatabaseObservability;
using ViajantesTurismo.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var options = builder.Configuration
    .GetSection(PostgreSqlIndexHealthHostOptions.SectionName)
    .Get<PostgreSqlIndexHealthHostOptions>() ?? new PostgreSqlIndexHealthHostOptions();

if (options.Enabled)
{
    var adminConnectionString = builder.Configuration.GetConnectionString(PostgreSqlIndexHealthHostOptions.AdminConnectionStringName);
    var catalogConnectionString = builder.Configuration.GetConnectionString(PostgreSqlIndexHealthHostOptions.CatalogConnectionStringName);
    if (string.IsNullOrWhiteSpace(adminConnectionString) || string.IsNullOrWhiteSpace(catalogConnectionString))
    {
        throw new InvalidOperationException("Enabled PostgreSQL index-health monitoring requires dedicated Admin and Catalog connection strings.");
    }

    builder.Services.AddPostgreSqlIndexHealthMonitoring(
    [
        adminConnectionString,
        catalogConnectionString,
    ],
    new PostgreSqlIndexHealthMonitoringOptions
    {
        PollingInterval = options.PollingInterval,
        CommandTimeout = options.CommandTimeout,
    });

    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics => metrics.AddMeter(PostgreSqlIndexHealthTelemetry.MeterName));
}

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
