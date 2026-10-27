using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PandaBear.Observability.Npgsql;
using ViajantesTurismo.ServiceDefaults;

namespace ViajantesTurismo.DatabaseObservability;

/// <summary>Configures the database observability host.</summary>
internal static class DatabaseObservabilityHostConfiguration
{
    /// <summary>Adds service defaults and, when enabled, PostgreSQL index-health monitoring for the Admin and Catalog databases.</summary>
    public static void Configure(IHostApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        var options = builder.Configuration
            .GetSection(PostgreSqlIndexHealthHostOptions.SectionName)
            .Get<PostgreSqlIndexHealthHostOptions>() ?? new PostgreSqlIndexHealthHostOptions();

        if (!options.Enabled)
        {
            return;
        }

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
}
