namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds the trusted local telemetry gateway and optional observability backends to the Aspire model.
/// </summary>
internal static class ObservabilityStackResourceExtensions
{
    /// <summary>
    /// Routes AppHost telemetry through a trusted OpenTelemetry Collector and optionally adds the local Grafana LGTM stack.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <remarks>
    /// The collector gateway is always present. Set <c>ASPIRE_ENABLE_OBSERVABILITY_STACK=1</c> before
    /// AppHost startup to add the local developer backends; otherwise sanitized telemetry is routed only
    /// to the built-in Aspire dashboard.
    /// </remarks>
    public static void AddObservabilityStack(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
        var observabilityRoot = Path.Combine(repositoryRoot, "observability");

        if (IsEnabled(Environment.GetEnvironmentVariable(GrafanaLgtmStackDefaults.EnableObservabilityStackVariable)))
        {
            builder.AddGrafanaLgtmStack(observabilityRoot);
            return;
        }

        builder.AddOpenTelemetryCollectorGateway(
            GrafanaLgtmStackDefaults.ResourceNames.OpenTelemetryCollector,
            Path.Combine(observabilityRoot, "otel-collector", "privacy.yaml"),
            Path.Combine(observabilityRoot, "otel-collector", "aspire.yaml"));
    }

    private static bool IsEnabled(string? value)
    {
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
