namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds optional local observability resources to the Aspire model.
/// </summary>
internal static class ObservabilityStackResourceExtensions
{
    /// <summary>
    /// Adds the optional local Grafana LGTM stack and routes AppHost telemetry through an OpenTelemetry Collector.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <remarks>
    /// Set <c>ASPIRE_ENABLE_OBSERVABILITY_STACK=1</c> before AppHost startup to include this local
    /// developer stack. The stack stays disabled by default because regular AppHost runs can use the
    /// built-in Aspire dashboard without starting backend containers.
    /// </remarks>
    public static void AddObservabilityStack(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!IsEnabled(Environment.GetEnvironmentVariable(GrafanaLgtmStackDefaults.EnableObservabilityStackVariable)))
        {
            return;
        }

        var repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
        var observabilityRoot = Path.Combine(repositoryRoot, "observability");

        builder.AddGrafanaLgtmStack(observabilityRoot);
    }

    private static bool IsEnabled(string? value)
    {
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
