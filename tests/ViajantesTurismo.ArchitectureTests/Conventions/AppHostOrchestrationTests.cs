using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed partial class AppHostOrchestrationTests
{
    [Fact]
    public void Aspire_release_docs_keep_versions_out_of_the_apphost()
    {
        // Arrange
        var appHostReadmeText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "README.md"));

        var releaseWorkflowDocsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "docs",
            "ci",
            "supplemental-workflows.md"));

        var releaseWorkflowText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            ".github",
            "workflows",
            "release-prep.yml"));

        var deploymentDocsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "docs",
            "architecture",
            "runtime-wiring-and-deployment.md"));

        var releasePublisherText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "DevelopmentProjectResourceExtensions.cs"));

        // Act
        var combinedDocsText = string.Join('\n', appHostReadmeText, releaseWorkflowDocsText, deploymentDocsText);

        // Assert
        appHostReadmeText.ShouldContain("The AppHost must not calculate application versions.", StringComparison.Ordinal);
        releaseWorkflowDocsText.ShouldContain("AppHost code stays", StringComparison.Ordinal);
        releaseWorkflowDocsText.ShouldContain("orchestration model", StringComparison.Ordinal);
        releaseWorkflowDocsText.ShouldContain("src/ViajantesTurismo.AppHost/README.md", StringComparison.Ordinal);
        appHostReadmeText.ShouldContain("docs/ci/supplemental-workflows.md", StringComparison.Ordinal);
        deploymentDocsText.ShouldContain("Release workflows own version and registry values", StringComparison.Ordinal);
        releaseWorkflowText.ShouldContain("calculate-release", StringComparison.Ordinal);
        releaseWorkflowText.ShouldContain("package_version", StringComparison.Ordinal);
        releaseWorkflowText.ShouldContain("VT_ASPIRE_CONTAINER_IMAGE_TAG", StringComparison.Ordinal);
        releaseWorkflowText.ShouldContain("ComputedInformationalVersion", StringComparison.Ordinal);
        releaseWorkflowText.ShouldContain("dotnet tool run aspire -- publish", StringComparison.Ordinal);
        combinedDocsText.ShouldContain("SharedKernel.Versioning.Tool calculate-release", StringComparison.Ordinal);
        combinedDocsText.ShouldContain("PublishAsDockerFile", StringComparison.Ordinal);
        combinedDocsText.ShouldContain("WithImageTag", StringComparison.Ordinal);
        combinedDocsText.ShouldContain("WithImageRegistry", StringComparison.Ordinal);
        combinedDocsText.ShouldContain("WithImagePushOptions", StringComparison.Ordinal);
        combinedDocsText.ShouldContain("WithManifestPublishingCallback", StringComparison.Ordinal);
        combinedDocsText.ShouldContain("OpenTelemetry `service.version`", StringComparison.Ordinal);
        combinedDocsText.ShouldContain("Infrastructure image tags and SHA-256 digests remain", StringComparison.Ordinal);
        releasePublisherText.ShouldContain("PublishAsDockerFile", StringComparison.Ordinal);
        releasePublisherText.ShouldContain("WithImageTag(", StringComparison.Ordinal);
        releasePublisherText.ShouldContain("WithImageRegistry(", StringComparison.Ordinal);
        releasePublisherText.ShouldContain("!HasContainerImageTag(builder)", StringComparison.Ordinal);
        releasePublisherText.ShouldContain("VT_DEPLOYMENT_VERSION", StringComparison.Ordinal);
        releasePublisherText.ShouldContain("VT_SOURCE_REVISION", StringComparison.Ordinal);
        appHostReadmeText.ShouldNotContain("git describe");
        releasePublisherText.ShouldNotContain("git describe");
        releasePublisherText.ShouldNotContain("git log");
        releasePublisherText.ShouldNotContain("git tag");
        releasePublisherText.ShouldNotContain("git rev-parse");
    }

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
        Assert.Contains("WithReference(catalogDatabase)", catalogApiBlock, StringComparison.Ordinal);
        Assert.Contains("WaitFor(catalogDatabase)", catalogApiBlock, StringComparison.Ordinal);
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

        var sharedHostingText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "SharedKernel",
            "SharedKernel.Aspire.Hosting.Grafana",
            "GrafanaLgtmStackResourceExtensions.cs"));

        var sharedDefaultsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "SharedKernel",
            "SharedKernel.Aspire.Hosting.Grafana",
            "GrafanaLgtmStackDefaults.cs"));

        // Act
        var hasGate = appHostText.Contains("GrafanaLgtmStackDefaults.EnableObservabilityStackVariable", StringComparison.Ordinal)
            && sharedDefaultsText.Contains("ASPIRE_ENABLE_OBSERVABILITY_STACK", StringComparison.Ordinal)
            && appHostText.Contains("AddObservabilityStack", StringComparison.Ordinal);

        // Assert
        Assert.True(hasGate);
        Assert.Contains("AddGrafanaLgtmStack", appHostText, StringComparison.Ordinal);
        Assert.DoesNotContain("ViajantesTurismo.Resources", appHostText, StringComparison.Ordinal);
        Assert.Contains("GrafanaLgtmStackDefaults.ResourceNames", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("\"opentelemetry-collector\"", sharedDefaultsText, StringComparison.Ordinal);
        Assert.Contains("\"grafana\"", sharedDefaultsText, StringComparison.Ordinal);
        Assert.Contains("\"loki\"", sharedDefaultsText, StringComparison.Ordinal);
        Assert.Contains("\"tempo\"", sharedDefaultsText, StringComparison.Ordinal);
        Assert.Contains("\"prometheus\"", sharedDefaultsText, StringComparison.Ordinal);
        Assert.Contains("AddOpenTelemetryCollector(resourceNames.OpenTelemetryCollector)", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("WithAppForwarding()", sharedHostingText, StringComparison.Ordinal);
        Assert.Equal(5, sharedHostingText.Split("ExcludeFromManifest()").Length - 1);
        Assert.DoesNotContain("AddContainer(", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("AddGrafana(resourceNames.Grafana", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("AddLoki(resourceNames.Loki", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("AddTempo(resourceNames.Tempo", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("AddPrometheus(", sharedHostingText, StringComparison.Ordinal);
    }

    [Fact]
    public void Observability_stack_uses_pinned_container_images()
    {
        // Arrange
        var sharedHostingText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "SharedKernel",
            "SharedKernel.Aspire.Hosting.Grafana",
            "GrafanaLgtmStackResourceExtensions.cs"));

        // Act
        var imageDigestCalls = sharedHostingText.Split("WithImageSHA256(").Length - 1;

        // Assert
        Assert.Equal(5, imageDigestCalls);
        Assert.Contains("OpenTelemetryCollectorImageDigest", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("GrafanaImageDigest", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("LokiImageDigest", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("TempoImageDigest", sharedHostingText, StringComparison.Ordinal);
        Assert.Contains("PrometheusImageDigest", sharedHostingText, StringComparison.Ordinal);
    }

}
