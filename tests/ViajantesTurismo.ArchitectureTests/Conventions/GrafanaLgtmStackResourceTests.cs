using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class GrafanaLgtmStackResourceTests
{
    [Fact]
    public void Default_resource_names_match_the_documented_local_stack()
    {
        // Arrange
        var names = GrafanaLgtmStackDefaults.ResourceNames;

        // Act
        var configuredNames = new[]
        {
            names.OpenTelemetryCollector,
            names.Grafana,
            names.Loki,
            names.Tempo,
            names.Prometheus,
        };

        // Assert
        Assert.Equal("ASPIRE_ENABLE_OBSERVABILITY_STACK", GrafanaLgtmStackDefaults.EnableObservabilityStackVariable);
        Assert.Collection(
            configuredNames,
            name => Assert.Equal("opentelemetry-collector", name),
            name => Assert.Equal("grafana", name),
            name => Assert.Equal("loki", name),
            name => Assert.Equal("tempo", name),
            name => Assert.Equal("prometheus", name));
    }

    [Fact]
    public void Add_grafana_lgtm_stack_adds_the_expected_resources()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        var resourceNames = new GrafanaLgtmStackResourceNames(
            "collector-test",
            "grafana-test",
            "loki-test",
            "tempo-test",
            "prometheus-test");

        // Act
        var grafana = builder.AddGrafanaLgtmStack(resourceNames, Path.Combine(Path.GetTempPath(), "grafana-lgtm-test"));
        var resources = builder.Resources.ToArray();

        // Assert
        Assert.IsType<GrafanaResource>(grafana.Resource);
        Assert.Contains(resources, resource => resource is GrafanaResource && resource.Name == "grafana-test");
        Assert.Contains(resources, resource => resource is LokiResource && resource.Name == "loki-test");
        Assert.Contains(resources, resource => resource is TempoResource && resource.Name == "tempo-test");
        Assert.Contains(resources, resource => resource is PrometheusResource && resource.Name == "prometheus-test");
        Assert.Contains(resources, resource => resource.Name == "collector-test");
    }
}
