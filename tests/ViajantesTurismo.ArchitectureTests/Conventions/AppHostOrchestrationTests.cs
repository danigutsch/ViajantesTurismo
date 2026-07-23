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
    public void Aspire_test_application_bounds_build_start_and_resource_health_waits()
    {
        // Arrange
        var testApplicationText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "SharedKernel",
            "SharedKernel.IntegrationTesting",
            "AspireTestApplication.cs"));

        // Act
        var usesBoundedStartup = testApplicationText.Contains("RunWithResourceStartupTimeout", StringComparison.Ordinal)
            && testApplicationText.Contains("BuildAsync(startupCt)", StringComparison.Ordinal)
            && testApplicationText.Contains("await builtApp.StartAsync(startupCt)", StringComparison.Ordinal)
            && testApplicationText.Contains(
                "WaitForResourceHealthyAsync(resourceName, startupCt)",
                StringComparison.Ordinal);

        // Assert
        usesBoundedStartup.ShouldBeTrue();
        testApplicationText.ShouldContain(
            "CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token)",
            StringComparison.Ordinal);
    }

    [Fact]
    public void Aspire_test_application_awaits_uncancellable_resource_teardown()
    {
        // Arrange
        var testApplicationText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "SharedKernel",
            "SharedKernel.IntegrationTesting",
            "AspireTestApplication.cs"));

        // Act
        var awaitsUncancellableTeardown = testApplicationText.Contains(
            "RunWithResourceTeardownTimeout(app.StopAsync)",
            StringComparison.Ordinal)
            && testApplicationText.Contains("await operation(timeoutCts.Token)", StringComparison.Ordinal)
            && testApplicationText.Contains(
                "CaptureTeardownFailure(() => app.DisposeAsync().AsTask()",
                StringComparison.Ordinal)
            && testApplicationText.Contains(
                "CaptureTeardownFailure(() => appBuilder.DisposeAsync().AsTask()",
                StringComparison.Ordinal);

        // Assert
        awaitsUncancellableTeardown.ShouldBeTrue();
        testApplicationText.ShouldNotContain(".WaitAsync(", StringComparison.Ordinal);
        testApplicationText.ShouldContain("CaptureTeardownFailure", StringComparison.Ordinal);
        testApplicationText.ShouldContain("new AggregateException(", StringComparison.Ordinal);
        testApplicationText.ShouldContain("var teardownFailures = await DisposeAfterFailedStart", StringComparison.Ordinal);
    }

    [Fact]
    public void Aspire_test_application_preserves_aspire_testing_concurrency_defaults()
    {
        // Arrange
        var repositoryRoot = GetRepositoryRoot();
        var testApplicationText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "SharedKernel",
            "SharedKernel.IntegrationTesting",
            "AspireTestApplication.cs"));
        var endpointLeasePath = Path.Combine(
            repositoryRoot,
            "src",
            "SharedKernel",
            "SharedKernel.IntegrationTesting",
            "AspireTestEndpointLease.cs");
        var publicApiText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "SharedKernel",
            "SharedKernel.IntegrationTesting",
            "PublicAPI.Unshipped.txt"));

        // Act
        var usesTestingBuilder = testApplicationText.Contains(
            "DistributedApplicationTestingBuilder",
            StringComparison.Ordinal);

        // Assert
        usesTestingBuilder.ShouldBeTrue();
        File.Exists(endpointLeasePath).ShouldBeFalse();
        testApplicationText.ShouldNotContain("AspireTestEndpointLease", StringComparison.Ordinal);
        testApplicationText.ShouldNotContain("DcpPublisher:RandomizePorts", StringComparison.Ordinal);
        publicApiText.ShouldNotContain(
            "Aspire.Hosting.IDistributedApplicationBuilder! builder",
            StringComparison.Ordinal);
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
        sharedHostingText.ShouldContain("AddOpenTelemetryCollectorGateway(", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("ConfigureAppForwarding(collector)", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("ResolveCollectorEndpointName", StringComparison.Ordinal);
        (sharedHostingText.Split("ExcludeFromManifest()").Length - 1).ShouldBe(5);
        sharedHostingText.ShouldNotContain("AddContainer(", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("AddGrafana(resourceNames.Grafana", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("AddLoki(resourceNames.Loki", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("AddTempo(resourceNames.Tempo", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("AddPrometheus(", StringComparison.Ordinal);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
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
        var sharedHostingText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "SharedKernel",
            "SharedKernel.Aspire.Hosting.Grafana",
            "GrafanaLgtmStackResourceExtensions.cs"));

        // Act
        var gatewayCallIndex = compositionText.IndexOf("builder.AddObservabilityStack();", StringComparison.Ordinal);
        var adminProfileReturnIndex = compositionText.IndexOf("if (profile is HostedProfile.Admin)", StringComparison.Ordinal);
        var gatewayCallCount = compositionText.Split("builder.AddObservabilityStack();", StringSplitOptions.None).Length - 1;

        // Assert
        gatewayCallIndex.ShouldBeGreaterThan(-1);
        gatewayCallIndex.ShouldBeLessThan(adminProfileReturnIndex);
        gatewayCallCount.ShouldBe(1);
        observabilityStackText.ShouldContain("AddOpenTelemetryCollectorGateway", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("WithConfig(privacyConfigurationFile)", StringComparison.Ordinal);
        sharedHostingText.ShouldContain("WithConfig(routingConfigurationFile)", StringComparison.Ordinal);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
    public void Collector_trace_pipeline_drops_events_and_sanitizes_spans_before_export()
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
        var aspirePipelineUsesPrivacyProcessors = aspireRoutingConfig.Contains(
            "processors: [memory_limiter, transform/sanitize_traces, filter/drop_span_events, batch]",
            StringComparison.Ordinal);
        var backendPipelineUsesPrivacyProcessors = backendRoutingConfig.Contains(
            "processors: [memory_limiter, transform/sanitize_traces, filter/drop_span_events, batch]",
            StringComparison.Ordinal);

        // Assert
        aspirePipelineUsesPrivacyProcessors.ShouldBeTrue();
        backendPipelineUsesPrivacyProcessors.ShouldBeTrue();
        privacyConfig.ShouldContain("memory_limiter:", StringComparison.Ordinal);
        privacyConfig.ShouldContain("limit_mib: 256", StringComparison.Ordinal);
        privacyConfig.ShouldContain("spike_limit_mib: 64", StringComparison.Ordinal);
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
        privacyConfig.ShouldContain("delete_matching_keys(resource.attributes, \"(?i)(^|[._-])(authorization|cookie|password|token|api[._-]?key|secret)([._-]|$)\")", StringComparison.Ordinal);
        privacyConfig.ShouldNotContain("set(span.name", StringComparison.Ordinal);
        privacyConfig.ShouldNotContain("delete_key(span.attributes, \"http.route\")", StringComparison.Ordinal);
        privacyConfig.ShouldNotContain("delete_key(span.attributes, \"error.type\")", StringComparison.Ordinal);
        aspireRoutingConfig.ShouldContain("exporters: [otlp/aspire]", StringComparison.Ordinal);
        backendRoutingConfig.ShouldContain("exporters: [otlp/aspire, otlp/tempo]", StringComparison.Ordinal);
        (aspireRoutingConfig.Split("processors: [memory_limiter, batch]", StringSplitOptions.None).Length - 1).ShouldBe(2);
        (backendRoutingConfig.Split("processors: [memory_limiter, batch]", StringSplitOptions.None).Length - 1).ShouldBe(2);
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
    public void Media_object_storage_source_configuration_is_private_and_pinned()
    {
        // Arrange
        var appHostExtensionsText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost", "AppHostResourceExtensions.cs"));
        var seaweedFsText = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "src", "SharedKernel", "SharedKernel.Aspire.Hosting.SeaweedFs", "SeaweedFsResourceExtensions.cs"));

        // Assert
        appHostExtensionsText.ShouldContain("AddMediaObjectStorage(this IDistributedApplicationBuilder builder)", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("\"viajantes-media\"", StringComparison.Ordinal);
        appHostExtensionsText.ShouldNotContain("$\"{ResourceNames.SeaweedFs}-bucket\"", StringComparison.Ordinal);
        appHostExtensionsText.ShouldContain("AddSeaweedFs(ResourceNames.SeaweedFs, SeaweedFsBucketDefault)", StringComparison.Ordinal);
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
        seaweedFsText.ShouldContain("DcpPublisher:ResourceNameSuffix", StringComparison.Ordinal);
    }

    [Fact]
    public void Seaweedfs_uses_named_storage()
    {
        // Arrange
        var seaweedFsText = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "src",
            "SharedKernel",
            "SharedKernel.Aspire.Hosting.SeaweedFs",
            "SeaweedFsResourceExtensions.cs"));

        // Act
        var usesNamedStorage = seaweedFsText.Contains("DcpPublisher:ResourceNameSuffix", StringComparison.Ordinal)
            && seaweedFsText.Contains("WithVolume(dataVolumeName, \"/data\")", StringComparison.Ordinal);

        // Assert
        usesNamedStorage.ShouldBeTrue();
    }

}
