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

    [Fact]
    public async Task Collector_gateway_routes_http_protobuf_to_the_http_endpoint_and_adds_a_health_wait()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        var configurationRoot = Path.Combine(Path.GetTempPath(), "collector-routing-test");
        var collector = builder.AddOpenTelemetryCollectorGateway(
            "collector-test",
            Path.Combine(configurationRoot, "privacy.yaml"),
            Path.Combine(configurationRoot, "aspire.yaml"));
        var sender = builder.AddContainer("sender-test", "test-image")
            .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        GrafanaLgtmStackResourceExtensions.ConfigureOpenTelemetryCollectorRouting(sender, collector);
        var environmentVariables = new Dictionary<string, object>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);

        // Act
        foreach (var annotation in sender.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(new EnvironmentCallbackContext(
                executionContext,
                sender.Resource,
                environmentVariables,
                TestContext.Current.CancellationToken));
        }

        var wait = sender.Resource.Annotations.OfType<WaitAnnotation>().ShouldHaveSingleItem();

        // Assert
        environmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"].ShouldBe("http/protobuf");
        var endpoint = environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"].ShouldBeOfType<EndpointReference>();
        endpoint.Resource.ShouldBeSameAs(collector.Resource);
        endpoint.EndpointName.ShouldBe("http");
        wait.Resource.ShouldBeSameAs(collector.Resource);
        wait.WaitType.ShouldBe(WaitType.WaitUntilHealthy);
    }

    [Fact]
    public async Task Add_grafana_enables_anonymous_local_access()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);

        // Act
        var grafana = builder.AddGrafana("grafana-test", Path.Combine(Path.GetTempPath(), "grafana-lgtm-test"));
        var environmentVariables = new Dictionary<string, object>();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        foreach (var annotation in grafana.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(new EnvironmentCallbackContext(
                executionContext,
                grafana.Resource,
                environmentVariables,
                TestContext.Current.CancellationToken));
        }

        // Assert
        var anonymousAccess = environmentVariables
            .ShouldHaveSingleItem(variable => string.Equals(
                variable.Key,
                "GF_AUTH_ANONYMOUS_ENABLED",
                StringComparison.Ordinal));
        anonymousAccess.Value.ShouldBe("true");
    }
}
