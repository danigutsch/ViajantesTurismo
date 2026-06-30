using ViajantesTurismo.Resources;

namespace ViajantesTurismo.AppHost;

/// <summary>
/// Adds optional local observability resources to the Aspire model.
/// </summary>
internal static class ObservabilityStackResourceExtensions
{
    private const string EnableObservabilityStackVariable = "VT_ASPIRE_ENABLE_OBSERVABILITY_STACK";

    /// <summary>Tag for <c>docker.io/otel/opentelemetry-collector-contrib:0.130.1</c>.</summary>
    private const string OpenTelemetryCollectorImageTag = "0.130.1";

    /// <summary>Digest for <c>docker.io/otel/opentelemetry-collector-contrib:0.130.1</c>.</summary>
    private const string OpenTelemetryCollectorImageDigest = "9c247564e65ca19f97d891cca19a1a8d291ce631b890885b44e3503c5fdb3895";

    /// <summary>Tag for <c>docker.io/grafana/grafana:12.0.2</c>.</summary>
    private const string GrafanaImageTag = "12.0.2";

    /// <summary>Digest for <c>docker.io/grafana/grafana:12.0.2</c>.</summary>
    private const string GrafanaImageDigest = "b5b59bfc7561634c2d7b136c4543d702ebcc94a3da477f21ff26f89ffd4214fa";

    /// <summary>Tag for <c>docker.io/grafana/loki:3.5.1</c>.</summary>
    private const string LokiImageTag = "3.5.1";

    /// <summary>Digest for <c>docker.io/grafana/loki:3.5.1</c>.</summary>
    private const string LokiImageDigest = "a74594532eec4cc313401beedc4dd2708c43674c032084b1aeb87c14a5be1745";

    /// <summary>Tag for <c>docker.io/grafana/tempo:2.8.1</c>.</summary>
    private const string TempoImageTag = "2.8.1";

    /// <summary>Digest for <c>docker.io/grafana/tempo:2.8.1</c>.</summary>
    private const string TempoImageDigest = "bc9245fe3da4e63dc4c6862d9c2dad9bcd8be13d0ba4f7705fa6acda4c904d0e";

    /// <summary>Tag for <c>docker.io/prom/prometheus:v3.5.0</c>.</summary>
    private const string PrometheusImageTag = "v3.5.0";

    /// <summary>Digest for <c>docker.io/prom/prometheus:v3.5.0</c>.</summary>
    private const string PrometheusImageDigest = "63805ebb8d2b3920190daf1cb14a60871b16fd38bed42b857a3182bc621f4996";

    /// <summary>
    /// Adds the optional local Grafana LGTM stack and routes AppHost telemetry through an OpenTelemetry Collector.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <remarks>
    /// Set <c>VT_ASPIRE_ENABLE_OBSERVABILITY_STACK=1</c> before AppHost startup to include this local
    /// developer stack. The stack stays disabled by default because regular AppHost runs can use the
    /// built-in Aspire dashboard without starting backend containers.
    /// </remarks>
    public static void AddObservabilityStack(this IDistributedApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!IsEnabled(Environment.GetEnvironmentVariable(EnableObservabilityStackVariable)))
        {
            return;
        }

        var repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
        var observabilityRoot = Path.Combine(repositoryRoot, "observability");
        var grafanaRoot = Path.Combine(observabilityRoot, "grafana");

        var tempo = builder.AddContainer(ResourceNames.Tempo, "grafana/tempo")
            .WithImageTag(TempoImageTag)
            .WithImageSHA256(TempoImageDigest)
            .WithBindMount(Path.Combine(observabilityRoot, "tempo", "tempo.yaml"), "/etc/tempo/tempo.yaml", isReadOnly: true)
            .WithArgs("-config.file=/etc/tempo/tempo.yaml")
            .WithEndpoint(targetPort: 4317, name: "otlp-grpc");

        var loki = builder.AddContainer(ResourceNames.Loki, "grafana/loki")
            .WithImageTag(LokiImageTag)
            .WithImageSHA256(LokiImageDigest)
            .WithBindMount(Path.Combine(observabilityRoot, "loki", "loki.yaml"), "/etc/loki/loki.yaml", isReadOnly: true)
            .WithArgs("-config.file=/etc/loki/loki.yaml")
            .WithHttpEndpoint(targetPort: 3100);

        var collector = builder.AddOpenTelemetryCollector(ResourceNames.OpenTelemetryCollector)
            .WithImageTag(OpenTelemetryCollectorImageTag)
            .WithImageSHA256(OpenTelemetryCollectorImageDigest)
            .WithConfig(Path.Combine(observabilityRoot, "otel-collector", "config.yaml"))
            .WithAppForwarding()
            .WaitFor(tempo)
            .WaitFor(loki);

        var prometheus = builder.AddContainer(ResourceNames.Prometheus, "prom/prometheus")
            .WithImageTag(PrometheusImageTag)
            .WithImageSHA256(PrometheusImageDigest)
            .WithBindMount(Path.Combine(observabilityRoot, "prometheus", "prometheus.yml"), "/etc/prometheus/prometheus.yml", isReadOnly: true)
            .WithArgs("--config.file=/etc/prometheus/prometheus.yml")
            .WithHttpEndpoint(targetPort: 9090)
            .WaitFor(collector);

        builder.AddContainer(ResourceNames.Grafana, "grafana/grafana")
            .WithImageTag(GrafanaImageTag)
            .WithImageSHA256(GrafanaImageDigest)
            .WithBindMount(Path.Combine(grafanaRoot, "provisioning"), "/etc/grafana/provisioning", isReadOnly: true)
            .WithBindMount(Path.Combine(grafanaRoot, "dashboards"), "/var/lib/grafana/dashboards", isReadOnly: true)
            .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
            .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
            .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
            .WithHttpEndpoint(targetPort: 3000)
            .WaitFor(loki)
            .WaitFor(tempo)
            .WaitFor(prometheus);
    }

    private static bool IsEnabled(string? value)
    {
        return string.Equals(value, "1", StringComparison.Ordinal)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
