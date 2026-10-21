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
        combinedDocsText.ShouldContain("sharedkernel-version calculate-release", StringComparison.Ordinal);
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
        var compositionText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostComposition.cs"));
        var resourceExtensionsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostResourceExtensions.cs"));

        // Act
        var hasReleaseGate = compositionText.Contains(
            "builder.AddRunModeIdentityProvider(managementWebClientSecret)",
            StringComparison.Ordinal);

        // Assert
        hasReleaseGate.ShouldBeTrue();
        resourceExtensionsText.ShouldContain("builder.ExecutionContext.IsRunMode", StringComparison.Ordinal);
        resourceExtensionsText.ShouldContain("WithLocalIdentityProvider", StringComparison.Ordinal);
    }

    [Fact]
    public void Local_oidc_provider_uses_an_explicit_registry_and_pinned_image()
    {
        // Arrange
        var resourceExtensionsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostResourceExtensions.cs"));

        // Act
        var identityProviderBlock = IdentityProviderResourceRegex().Match(resourceExtensionsText).Value;

        // Assert
        identityProviderBlock.ShouldNotBeEmpty();
        identityProviderBlock.ShouldContain(
            "builder.AddContainer(ResourceNames.IdentityProvider, KeycloakImageName)",
            StringComparison.Ordinal);
        identityProviderBlock.ShouldContain(".WithImageRegistry(KeycloakImageRegistry)", StringComparison.Ordinal);
        identityProviderBlock.ShouldContain(".WithImageTag(KeycloakImageTag)", StringComparison.Ordinal);
        identityProviderBlock.ShouldContain(".WithImageSHA256(OidcProviderImageDigest)", StringComparison.Ordinal);
        resourceExtensionsText.ShouldContain(
            "private const string KeycloakImageRegistry = \"quay.io\";",
            StringComparison.Ordinal);
        resourceExtensionsText.ShouldContain(
            "private const string KeycloakImageName = \"keycloak/keycloak\";",
            StringComparison.Ordinal);
        resourceExtensionsText.ShouldContain(
            "private const string KeycloakImageTag = \"26.7.0\";",
            StringComparison.Ordinal);
        resourceExtensionsText.ShouldContain(
            "private const string OidcProviderImageDigest = \"2eb3cd316835c990e69e26ade292ffa78f6fb0db7d5fc6377463c162e1979ac0\";",
            StringComparison.Ordinal);
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
    public void Management_security_schema_is_provisioned_before_the_web_starts()
    {
        // Arrange
        var resourceExtensionsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostResourceExtensions.cs"));

        // Assert
        resourceExtensionsText.ShouldContain("WithReference(securityDatabase)", StringComparison.Ordinal);
        resourceExtensionsText.ShouldContain("WaitFor(securityDatabase)", StringComparison.Ordinal);
        resourceExtensionsText.ShouldContain("WaitForCompletion(migrationService)", StringComparison.Ordinal);
    }

    [Fact]
    public void System_hosted_profile_includes_media_without_developer_tooling()
    {
        // Arrange
        var profileExtensionsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostProfileExtensions.cs"));

        // Act
        var includesSystemMedia = profileExtensionsText.Contains(
            "return profile is HostedProfile.Full or HostedProfile.System;",
            StringComparison.Ordinal);

        // Assert
        includesSystemMedia.ShouldBeTrue();
        profileExtensionsText.ShouldContain(
            "return profile is HostedProfile.Full;",
            StringComparison.Ordinal);
    }

    [Fact]
    public void System_fixture_allows_full_profile_resources_to_start()
    {
        // Arrange
        var fixtureText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "ViajantesTurismo.Admin.SystemTests",
            "Infrastructure",
            "Fixtures",
            "AspireSystemTestFixture.cs"));

        // Act
        var usesDedicatedStartupTimeout = fixtureText.Contains(
            "SystemResourceStartupTimeout",
            StringComparison.Ordinal)
            && fixtureText.Contains("TimeSpan.FromMinutes(3)", StringComparison.Ordinal);

        // Assert
        usesDedicatedStartupTimeout.ShouldBeTrue();
    }

    [Fact]
    public void Admin_integration_fixture_allows_admin_profile_resources_to_start()
    {
        // Arrange
        var fixtureText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "tests",
            "ViajantesTurismo.Admin.IntegrationTests",
            "Infrastructure",
            "ApiFixture.cs"));

        // Act
        var usesDedicatedStartupTimeout = fixtureText.Contains(
            "ApiResourceStartupTimeout",
            StringComparison.Ordinal)
            && fixtureText.Contains("TimeSpan.FromMinutes(3)", StringComparison.Ordinal)
            && fixtureText.Contains(
                "[ResourceNames.Api, ResourceNames.DatabaseServer],\n            ApiResourceStartupTimeout,\n            appHostArguments,",
                StringComparison.Ordinal);

        // Assert
        usesDedicatedStartupTimeout.ShouldBeTrue();
    }

    [Fact]
    public void Database_observability_waits_for_both_databases_after_migrations()
    {
        // Arrange
        var compositionText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostComposition.cs"));
        var appHostExtensionsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostResourceExtensions.cs"));
        var appHostProfileExtensionsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostProfileExtensions.cs"));

        // Act
        var databaseObservabilityBlock = DatabaseObservabilityResourceRegex().Match(appHostExtensionsText).Value;

        // Assert
        appHostProfileExtensionsText.ShouldContain("Aspire:Features:DatabaseObservability", StringComparison.Ordinal);
        compositionText.ShouldContain("AddDatabaseObservability(adminDatabase, catalogDatabase, migrationService)", StringComparison.Ordinal);
        databaseObservabilityBlock.ShouldNotBeEmpty();
        databaseObservabilityBlock.ShouldContain("AddDevelopmentDotNetProject<ViajantesTurismo_DatabaseObservability>", StringComparison.Ordinal);
        databaseObservabilityBlock.ShouldContain("WaitFor(adminDatabase)", StringComparison.Ordinal);
        databaseObservabilityBlock.ShouldContain("WaitFor(catalogDatabase)", StringComparison.Ordinal);
        databaseObservabilityBlock.ShouldContain("WaitForCompletion(migrationService)", StringComparison.Ordinal);
        databaseObservabilityBlock.ShouldContain("AddParameter(AdminIndexHealthConnectionStringParameterName, secret: true)", StringComparison.Ordinal);
        databaseObservabilityBlock.ShouldContain("AddParameter(CatalogIndexHealthConnectionStringParameterName, secret: true)", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("ConnectionStrings__admin-index-health", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("ConnectionStrings__catalog-index-health", StringComparison.Ordinal);
        databaseObservabilityBlock.ShouldNotContain("WithReference(adminDatabase)", StringComparison.Ordinal);
        databaseObservabilityBlock.ShouldNotContain("WithReference(catalogDatabase)", StringComparison.Ordinal);
    }

    [Fact]
    public void Observability_backends_are_opt_in_and_route_through_collector()
    {
        // Arrange
        var appHostText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "ObservabilityStackResourceExtensions.cs"));

        // Act
        var hasGate = appHostText.Contains("GrafanaLgtmStackDefaults.EnableObservabilityStackVariable", StringComparison.Ordinal)
            && appHostText.Contains("AddObservabilityStack", StringComparison.Ordinal);

        // Assert
        hasGate.ShouldBeTrue();
        appHostText.ShouldContain("AddGrafanaLgtmStack", StringComparison.Ordinal);
        appHostText.ShouldNotContain("ViajantesTurismo.Resources", StringComparison.Ordinal);
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
    public void Every_hosted_profile_routes_through_the_trusted_collector_gateway()
    {
        // Arrange
        var compositionText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "AppHostComposition.cs"));
        var observabilityStackText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "ViajantesTurismo.AppHost",
            "ObservabilityStackResourceExtensions.cs"));

        // Act
        var gatewayCallIndex = compositionText.IndexOf("builder.AddObservabilityStack();", StringComparison.Ordinal);
        var adminProfileReturnIndex = compositionText.IndexOf("if (profile is HostedProfile.Admin)", StringComparison.Ordinal);
        var gatewayCallCount = compositionText.Split("builder.AddObservabilityStack();", StringSplitOptions.None).Length - 1;

        // Assert
        gatewayCallIndex.ShouldBeGreaterThan(-1);
        gatewayCallIndex.ShouldBeLessThan(adminProfileReturnIndex);
        gatewayCallCount.ShouldBe(1);
        observabilityStackText.ShouldContain("AddOpenTelemetryCollectorGateway", StringComparison.Ordinal);
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
    public void Collector_pipelines_sanitize_resources_and_trace_payloads_before_export()
    {
        // Arrange
        var collectorRoot = Path.Combine(GetRepositoryRoot(), "observability", "otel-collector");
        var privacyConfig = File.ReadAllText(Path.Combine(collectorRoot, "privacy.yaml"))
            .ReplaceLineEndings("\n");
        var aspireRoutingConfig = File.ReadAllText(Path.Combine(collectorRoot, "aspire.yaml"))
            .ReplaceLineEndings("\n");
        var backendRoutingConfig = File.ReadAllText(Path.Combine(collectorRoot, "config.yaml"))
            .ReplaceLineEndings("\n");

        // Act
        var aspireTracePipelineUsesPrivacyProcessors = aspireRoutingConfig.Contains(
            "processors: [memory_limiter, transform/sanitize_resources, transform/sanitize_traces, filter/drop_span_events, batch]",
            StringComparison.Ordinal);
        var backendTracePipelineUsesPrivacyProcessors = backendRoutingConfig.Contains(
            "processors: [memory_limiter, transform/sanitize_resources, transform/sanitize_traces, filter/drop_span_events, batch]",
            StringComparison.Ordinal);
        const string nonTracePrivacyProcessors =
            "processors: [memory_limiter, transform/sanitize_resources, batch]";
        const string resourceAttributeSanitizer =
            "delete_matching_keys(resource.attributes, \"(?i)(^|[._-])(authorization|cookie|password|token|api[._-]?key|secret)([._-]|$)\")";

        // Assert
        aspireTracePipelineUsesPrivacyProcessors.ShouldBeTrue();
        backendTracePipelineUsesPrivacyProcessors.ShouldBeTrue();
        (aspireRoutingConfig.Split(nonTracePrivacyProcessors, StringSplitOptions.None).Length - 1).ShouldBe(2);
        (backendRoutingConfig.Split(nonTracePrivacyProcessors, StringSplitOptions.None).Length - 1).ShouldBe(2);
        privacyConfig.ShouldContain("memory_limiter:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("limit_mib: 256", StringComparison.Ordinal);
        privacyConfig.ShouldContain("spike_limit_mib: 64", StringComparison.Ordinal);
        privacyConfig.ShouldContain("transform/sanitize_resources:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("trace_statements:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("log_statements:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("metric_statements:", StringComparison.Ordinal);
        (privacyConfig.Split("- context: resource", StringSplitOptions.None).Length - 1).ShouldBe(3);
        (privacyConfig.Split(resourceAttributeSanitizer, StringSplitOptions.None).Length - 1).ShouldBe(3);
        privacyConfig.ShouldContain("filter/drop_span_events:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("spanevent:\n        - 'true'", StringComparison.Ordinal);
        privacyConfig.ShouldNotContain("\n      span:\n", StringComparison.Ordinal);
        privacyConfig.ShouldContain("transform/sanitize_traces:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("set(span.status.message, \"\")", StringComparison.Ordinal);
        privacyConfig.ShouldContain("delete_key(span.attributes, \"url.path\")", StringComparison.Ordinal);
        privacyConfig.ShouldContain("delete_key(span.attributes, \"db.query.text\")", StringComparison.Ordinal);
        privacyConfig.ShouldContain("delete_key(span.attributes, \"parameters\")", StringComparison.Ordinal);
        privacyConfig.ShouldContain("delete_matching_keys(span.attributes, \"(?i)(^|[._])headers?([._]|$)\")", StringComparison.Ordinal);
        privacyConfig.ShouldContain("delete_matching_keys(span.attributes, \"(?i)^aws\\\\.(s3|sqs|sns)\\\\.\")", StringComparison.Ordinal);
        privacyConfig.ShouldContain("delete_matching_keys(span.attributes, \"(?i)(^|[._-])(authorization|cookie|password|token|api[._-]?key|secret)([._-]|$)\")", StringComparison.Ordinal);
        privacyConfig.ShouldNotContain("set(span.name", StringComparison.Ordinal);
        privacyConfig.ShouldNotContain("delete_key(span.attributes, \"http.route\")", StringComparison.Ordinal);
        privacyConfig.ShouldNotContain("delete_key(span.attributes, \"error.type\")", StringComparison.Ordinal);
        aspireRoutingConfig.ShouldContain("exporters: [otlp/aspire]", StringComparison.Ordinal);
        backendRoutingConfig.ShouldContain("exporters: [otlp/aspire, otlp/tempo]", StringComparison.Ordinal);
    }

    [Fact]
    public void Media_object_storage_source_configuration_is_private_and_pinned()
    {
        // Arrange
        var appHostExtensionsText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost", "AppHostResourceExtensions.cs"));

        // Assert
        appHostExtensionsText.ShouldContain("AddMediaObjectStorage(this IDistributedApplicationBuilder builder)", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("\"viajantes-media\"", StringComparison.Ordinal);
        appHostExtensionsText.ShouldNotContain("$\"{ResourceNames.SeaweedFs}-bucket\"", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("AddSeaweedFs(ResourceNames.SeaweedFs, SeaweedFsBucketDefault)", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("private static IResourceBuilder<TDestination> WithSeaweedFsReference<TDestination>(", StringComparison.Ordinal);
    }

}
