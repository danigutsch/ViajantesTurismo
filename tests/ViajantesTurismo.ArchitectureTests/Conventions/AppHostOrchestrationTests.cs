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
        releasePublisherText.ShouldContain("builder.ExecutionContext.IsRunMode", StringComparison.Ordinal);
        releasePublisherText.ShouldContain("VT_DEPLOYMENT_VERSION", StringComparison.Ordinal);
        releasePublisherText.ShouldContain("VT_SOURCE_REVISION", StringComparison.Ordinal);
        appHostReadmeText.ShouldNotContain("git describe", StringComparison.Ordinal);
        releasePublisherText.ShouldNotContain("git describe", StringComparison.Ordinal);
        releasePublisherText.ShouldNotContain("git log", StringComparison.Ordinal);
        releasePublisherText.ShouldNotContain("git tag", StringComparison.Ordinal);
        releasePublisherText.ShouldNotContain("git rev-parse", StringComparison.Ordinal);
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
    public void Local_oidc_provider_is_excluded_from_release_publishing()
    {
        // Arrange
        var appHostText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHost.cs"));
        var resourceExtensionsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostResourceExtensions.cs"));

        // Act
        var hasReleaseGate = appHostText.Contains(
            "builder.AddRunModeIdentityProvider(managementWebClientSecret)",
            StringComparison.Ordinal);

        // Assert
        hasReleaseGate.ShouldBeTrue();
        resourceExtensionsText.ShouldContain("builder.ExecutionContext.IsRunMode", StringComparison.Ordinal);
        resourceExtensionsText.ShouldContain("WithLocalIdentityProvider", StringComparison.Ordinal);
    }

    [Fact]
    public void Local_postgresql_capacity_supports_the_system_test_resource_model()
    {
        // Arrange
        var resourceExtensionsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostResourceExtensions.cs"));

        // Act and assert
        resourceExtensionsText.ShouldContain("WithArgs(\"-c\", \"max_connections=200\")", StringComparison.Ordinal);
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
        appHostText.ShouldNotContain("ViajantesTurismo.Resources", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("GrafanaLgtmStackDefaults.ResourceNames", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"opentelemetry-collector\"", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"grafana\"", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"loki\"", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"tempo\"", StringComparison.Ordinal);
        sharedDefaultsText.ShouldContain("\"prometheus\"", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("AddOpenTelemetryCollector(resourceNames.OpenTelemetryCollector)", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("WithAppForwarding()", StringComparison.Ordinal);
        (sharedHostingText.Split("ExcludeFromManifest()").Length - 1).ShouldBe(5);
        sharedHostingText.ShouldNotContain("AddContainer(", StringComparison.Ordinal);
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

    [Fact]
    public void Media_infrastructure_resources_are_private_pinned_and_ready_before_catalog_services()
    {
        // Arrange
        var appHostText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost", "AppHost.cs"));
        var appHostExtensionsText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost", "AppHostResourceExtensions.cs"));
        var clamAvText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "SharedKernel", "SharedKernel.Aspire.Hosting.ClamAv", "ClamAvResourceExtensions.cs"));
        var clamAvHealthCheckText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "SharedKernel", "SharedKernel.Aspire.Hosting.ClamAv", "ClamAvPingHealthCheck.cs"));
        var seaweedFsText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "SharedKernel", "SharedKernel.Aspire.Hosting.SeaweedFs", "SeaweedFsResourceExtensions.cs"));

        // Act
        var catalogApiBlock = CatalogApiResourceRegex().Match(appHostExtensionsText).Value;

        // Assert
        appHostText.ShouldContain("AddClamAv(ResourceNames.ClamAv)", StringComparison.Ordinal);
        appHostText.ShouldContain("var seaweedFs = builder.AddMediaObjectStorage();", StringComparison.Ordinal);
        appHostText.ShouldNotContain("seaweedFsBucket", StringComparison.Ordinal);
        appHostText.ShouldNotContain("viajantes-media", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("AddMediaObjectStorage(this IDistributedApplicationBuilder builder)", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("\"viajantes-media\"", StringComparison.Ordinal);
        appHostExtensionsText.ShouldNotContain("$\"{ResourceNames.SeaweedFs}-bucket\"", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("AddSeaweedFs(ResourceNames.SeaweedFs, SeaweedFsBucketDefault)", StringComparison.Ordinal);
        catalogApiBlock.ShouldContain("WithClamAvReference(clamAv)", StringComparison.Ordinal);
        catalogApiBlock.ShouldContain("WaitFor(clamAv)", StringComparison.Ordinal);
        clamAvText.ShouldContain("WithImageSHA256", StringComparison.Ordinal);
        clamAvText.ShouldContain("isExternal: false", StringComparison.Ordinal);
        clamAvText.ShouldContain("WithVolume", StringComparison.Ordinal);
        clamAvText.ShouldContain("WithFreshClam", StringComparison.Ordinal);
        clamAvText.ShouldContain("CLAMAV_NO_FRESHCLAMD", StringComparison.Ordinal);
        clamAvHealthCheckText.ShouldContain("zPING", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("WithImageSHA256", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("isExternal: false", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("-dir=/data", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("-webdav=false", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("-admin.ui=false", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("WithHttpHealthCheck", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("SeaweedFsResource", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("secret: true", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("persist: true", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("AWS_ACCESS_KEY_ID", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("AWS_SECRET_ACCESS_KEY", StringComparison.Ordinal);
        seaweedFsText.ShouldContain("$\"{name}-bucket\"", StringComparison.Ordinal);
        seaweedFsText.ShouldNotContain("viajantes-media", StringComparison.Ordinal);
        seaweedFsText.ShouldNotContain("Catalog__MediaObjectStorage", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("private static IResourceBuilder<TDestination> WithSeaweedFsReference<TDestination>(", StringComparison.Ordinal);
    }

}
