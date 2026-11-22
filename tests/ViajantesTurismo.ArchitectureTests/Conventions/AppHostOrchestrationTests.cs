using static ViajantesTurismo.ArchitectureTests.Conventions.AppHostOrchestrationTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class AppHostOrchestrationTests
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
        releaseWorkflowText.ShouldContain("dotnet tool run aspire -- 'do' publish-manifest", StringComparison.Ordinal);
        releaseWorkflowText.ShouldContain("test -s artifacts/aspire/release-prep/aspire-manifest.json", StringComparison.Ordinal);
        releaseWorkflowText.ShouldContain("path: artifacts/aspire/release-prep/aspire-manifest.json", StringComparison.Ordinal);
        releaseWorkflowText.ShouldContain("if-no-files-found: error", StringComparison.Ordinal);
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
        var fixtureText = HostedTestFixtureSource.ReadNormalized();

        // Assert
        fixtureText.ShouldContain("StartBudget = TimeSpan.FromMinutes(3);", StringComparison.Ordinal, "the start of a stack is bounded");
        fixtureText.ShouldContain(
            "launch.Profile is HostedProfile.System ? [ResourceNames.Api, ResourceNames.WebApp, ResourceNames.PublicWebApp] : [",
            StringComparison.Ordinal,
            "the System stack awaits the API and both web applications");
    }

    [Fact]
    public void Admin_integration_fixture_allows_admin_profile_resources_to_start()
    {
        // Arrange
        var fixtureText = HostedTestFixtureSource.ReadNormalized();

        // Assert
        fixtureText.ShouldContain("StartBudget = TimeSpan.FromMinutes(3);", StringComparison.Ordinal, "the start of a stack is bounded");
        fixtureText.ShouldContain(
            ": [ResourceNames.Api, ResourceNames.CatalogApi]; return new AppHostLaunch([.. owner.Configuration.Arguments, .. launch.Profile.ToArguments()], healthy, StartBudget, configuration);",
            StringComparison.Ordinal,
            "the Admin stack awaits the API and the Catalog API within the start budget");
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
        var gatewayCallIndex = compositionText.IndexOf("builder.AddObservabilityStack(includeDeveloperTooling);", StringComparison.Ordinal);
        var adminProfileReturnIndex = compositionText.IndexOf("if (profile is HostedProfile.Admin)", StringComparison.Ordinal);
        var gatewayCallCount = compositionText.Split("builder.AddObservabilityStack(includeDeveloperTooling);", StringSplitOptions.None).Length - 1;

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
        var actualValue = aspireRoutingConfig.Split(nonTracePrivacyProcessors, StringSplitOptions.None).Length - 1;
        actualValue.ShouldBe(2);
        var actualValue1 = backendRoutingConfig.Split(nonTracePrivacyProcessors, StringSplitOptions.None).Length - 1;
        actualValue1.ShouldBe(2);
        privacyConfig.ShouldContain("memory_limiter:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("limit_mib: 256", StringComparison.Ordinal);
        privacyConfig.ShouldContain("spike_limit_mib: 64", StringComparison.Ordinal);
        privacyConfig.ShouldContain("transform/sanitize_resources:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("trace_statements:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("log_statements:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("metric_statements:", StringComparison.Ordinal);
        var actualValue2 = privacyConfig.Split("- context: resource", StringSplitOptions.None).Length - 1;
        actualValue2.ShouldBe(3);
        var actualValue3 = privacyConfig.Split(resourceAttributeSanitizer, StringSplitOptions.None).Length - 1;
        actualValue3.ShouldBe(3);
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
