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
        GrafanaLgtmStackDefaults.EnableObservabilityStackVariable.ShouldBe("ASPIRE_ENABLE_OBSERVABILITY_STACK");
        (configuredNames).ShouldMatchCollection(name => name.ShouldBe("opentelemetry-collector"), name => name.ShouldBe("grafana"), name => name.ShouldBe("loki"), name => name.ShouldBe("tempo"), name => name.ShouldBe("prometheus"));
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
        grafana.Resource.ShouldBeOfType<GrafanaResource>();
        resources.ShouldContain(resource => resource is GrafanaResource && resource.Name == "grafana-test");
        resources.ShouldContain(resource => resource is LokiResource && resource.Name == "loki-test");
        resources.ShouldContain(resource => resource is TempoResource && resource.Name == "tempo-test");
        resources.ShouldContain(resource => resource is PrometheusResource && resource.Name == "prometheus-test");
        resources.ShouldContain(resource => resource.Name == "collector-test");
    }
}
