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
    /// The collector gateway is always present. Set <c>ASPIRE_ENABLE_OBSERVABILITY_STACK=1</c> (an environment
    /// variable or any other AppHost configuration source) before AppHost startup to add the local developer
    /// backends; otherwise sanitized telemetry is routed only to the built-in Aspire dashboard. With an external collector configured
    /// under <see cref="ExternalInfrastructure.OtlpEndpointKey"/>, no collector is added and every project exports to that one.
    /// </remarks>
    public static void AddObservabilityStack(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (ExternalInfrastructure.OtlpEndpoint(builder.Configuration) is { } externalCollector)
        {
            builder.RouteTelemetryTo(externalCollector);
            return;
        }

        var repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
        var observabilityRoot = Path.Combine(repositoryRoot, "observability");

        if (IsEnabled(builder.Configuration[GrafanaLgtmStackDefaults.EnableObservabilityStackVariable]))
        {
            builder.AddGrafanaLgtmStack(observabilityRoot);
            builder.AddPostgreSqlServerMetrics(observabilityRoot);
            return;
        }

        builder.AddOpenTelemetryCollectorGateway(
            GrafanaLgtmStackDefaults.ResourceNames.OpenTelemetryCollector,
            Path.Combine(observabilityRoot, "otel-collector", "privacy.yaml"),
            Path.Combine(observabilityRoot, "otel-collector", "aspire.yaml"));
    }

    /// <summary>Sends the OTLP export of every project to an external collector over gRPC.</summary>
    private static void RouteTelemetryTo(this IDistributedApplicationBuilder builder, Uri collector)
    {
        builder.Eventing.Subscribe<BeforeStartEvent>((beforeStart, _) =>
        {
            foreach (var project in beforeStart.Model.Resources.OfType<ProjectResource>())
            {
                builder.CreateResourceBuilder(project)
                    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", collector.GetLeftPart(UriPartial.Authority))
                    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc");
            }

            return Task.CompletedTask;
        });
    }

    private static bool IsEnabled(string? value)
    {
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
