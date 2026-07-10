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
        catalogApiBlock.ShouldNotBeEmpty();
        catalogApiBlock.ShouldContain("WithReference(catalogDatabase)", StringComparison.Ordinal);
        catalogApiBlock.ShouldContain("WaitFor(catalogDatabase)", StringComparison.Ordinal);
        catalogApiBlock.ShouldContain("WaitForCompletion(migrationService)", StringComparison.Ordinal);
    }

    [Fact]
    public void AppHost_should_not_adopt_candidate_platform_services_without_an_adoption_issue()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();

        // Act
        var packageViolations = FindCandidatePlatformPackageReferenceViolations(repositoryRoot);
        var resourceViolations = FindCandidatePlatformResourceFragments(repositoryRoot);

        // Assert
        packageViolations.ShouldBe([]);
        resourceViolations.ShouldBe([]);
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
        hasGate.ShouldBeTrue();
        appHostText.ShouldContain("AddGrafanaLgtmStack", StringComparison.Ordinal);
        appHostText.ShouldNotContain("ViajantesTurismo.Resources");
        sharedHostingText.ShouldContain("GrafanaLgtmStackDefaults.ResourceNames", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"opentelemetry-collector\"", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"grafana\"", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"loki\"", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"tempo\"", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"prometheus\"", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("AddOpenTelemetryCollector(resourceNames.OpenTelemetryCollector)", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("WithAppForwarding()", StringComparison.Ordinal);
        (sharedHostingText.Split("ExcludeFromManifest()").Length - 1).ShouldBe(5);
        sharedHostingText.ShouldNotContain("AddContainer(");
        sharedHostingText.ShouldContain("AddGrafana(resourceNames.Grafana", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("AddLoki(resourceNames.Loki", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("AddTempo(resourceNames.Tempo", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("AddPrometheus(", StringComparison.Ordinal);
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
        imageDigestCalls.ShouldBe(5);
        sharedHostingText.ShouldContain("OpenTelemetryCollectorImageDigest", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("GrafanaImageDigest", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("LokiImageDigest", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("TempoImageDigest", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("PrometheusImageDigest", StringComparison.Ordinal);
    }

}
