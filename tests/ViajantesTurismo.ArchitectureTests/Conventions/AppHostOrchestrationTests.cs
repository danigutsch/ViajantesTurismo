using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AppHostOrchestrationTests
{
    [Fact]
    public void Catalog_api_waits_for_database_migrations_when_it_uses_persisted_public_content()
    {
        // Arrange
        var appHostText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostResourceExtensions.cs"));

        // Act
        var catalogApiBlock = CatalogApiResourceRegex().Match(appHostText).Value;

        // Assert
        Assert.NotEmpty(catalogApiBlock);
        Assert.Contains("WithReference(database)", catalogApiBlock, StringComparison.Ordinal);
        Assert.Contains("WaitFor(database)", catalogApiBlock, StringComparison.Ordinal);
        Assert.Contains("WaitForCompletion(migrationService)", catalogApiBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void Observability_stack_is_opt_in_and_routes_through_collector()
    {
        // Arrange
        var appHostText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "ObservabilityStackResourceExtensions.cs"));

        // Act
        var hasGate = appHostText.Contains("VT_ASPIRE_ENABLE_OBSERVABILITY_STACK", StringComparison.Ordinal)
            && appHostText.Contains("AddObservabilityStack", StringComparison.Ordinal);

        // Assert
        Assert.True(hasGate);
        Assert.Contains("AddOpenTelemetryCollector(ResourceNames.OpenTelemetryCollector)", appHostText, StringComparison.Ordinal);
        Assert.Contains("WithAppForwarding()", appHostText, StringComparison.Ordinal);
        Assert.Contains("AddContainer(ResourceNames.Grafana", appHostText, StringComparison.Ordinal);
        Assert.Contains("AddContainer(ResourceNames.Loki", appHostText, StringComparison.Ordinal);
        Assert.Contains("AddContainer(ResourceNames.Tempo", appHostText, StringComparison.Ordinal);
        Assert.Contains("AddContainer(ResourceNames.Prometheus", appHostText, StringComparison.Ordinal);
    }

    [Fact]
    public void Observability_stack_uses_pinned_container_images()
    {
        // Arrange
        var appHostText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "ObservabilityStackResourceExtensions.cs"));

        // Act
        var imageDigestCalls = appHostText.Split("WithImageSHA256(").Length - 1;

        // Assert
        Assert.Equal(5, imageDigestCalls);
        Assert.Contains("OpenTelemetryCollectorImageDigest", appHostText, StringComparison.Ordinal);
        Assert.Contains("GrafanaImageDigest", appHostText, StringComparison.Ordinal);
        Assert.Contains("LokiImageDigest", appHostText, StringComparison.Ordinal);
        Assert.Contains("TempoImageDigest", appHostText, StringComparison.Ordinal);
        Assert.Contains("PrometheusImageDigest", appHostText, StringComparison.Ordinal);
    }

}
