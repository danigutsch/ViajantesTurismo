using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>Adds the optional PostgreSQL server metrics configuration to the local Collector.</summary>
internal static class PostgreSqlServerMetricsResourceExtensions
{
    private const string EnabledKey = "Aspire:Features:PostgreSqlServerMetrics";

    /// <summary>
    /// Enables the metrics overlay for the AppHost-owned database server when explicitly configured.
    /// </summary>
    public static void AddPostgreSqlServerMetrics(this IDistributedApplicationBuilder builder, string observabilityRoot)
    {
        if (!string.Equals(builder.Configuration[EnabledKey], "true", StringComparison.OrdinalIgnoreCase)
            || ExternalInfrastructure.HasDatabases(builder.Configuration))
        {
            return;
        }

        var collector = builder.Resources.OfType<OpenTelemetryCollectorResource>()
            .Single(resource => resource.Name == GrafanaLgtmStackDefaults.ResourceNames.OpenTelemetryCollector);
        var username = builder.AddParameter(ResourceNames.PostgreSqlServerMetricsUsername, secret: true);
        var password = builder.AddParameter(ResourceNames.PostgreSqlServerMetricsPassword, secret: true);
        builder.CreateResourceBuilder(collector)
            .WithConfig(Path.Combine(observabilityRoot, "otel-collector", "postgresql-server.yaml"))
            .WithEnvironment("VT_POSTGRES_SERVER_METRICS_HOST", ResourceNames.DatabaseServer)
            .WithEnvironment("VT_POSTGRES_SERVER_METRICS_USER", username)
            .WithEnvironment("VT_POSTGRES_SERVER_METRICS_PASSWORD", password);
    }
}
