using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Verifies the publish-mode AppHost model without starting application processes or containers.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class AppHostPublishModelTests
{
    [Fact]
    public async Task Publish_mode_keeps_the_release_identity_on_all_application_build_resources()
    {
        // Arrange
        const string imageTag = "0.1.0-alpha.17.1";
        const string registry = "registry.example.test";
        const string deploymentVersion = "0.1.0-alpha.17.1+sha.abcdef0";
        const string sourceRevision = "abcdef0";
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest"],
            ProjectDirectory = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost"),
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["VT_ASPIRE_CONTAINER_IMAGE_TAG"] = imageTag,
            ["VT_ASPIRE_CONTAINER_REGISTRY"] = registry,
            ["VT_ASPIRE_DEPLOYMENT_VERSION"] = deploymentVersion,
            ["VT_ASPIRE_SOURCE_REVISION"] = sourceRevision
        });

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.Full);
        var buildResources = builder.Resources.OfType<ContainerResource>()
            .Where(resource => resource.Annotations.OfType<DockerfileBuildAnnotation>().Any())
            .ToArray();

        // Assert
        buildResources.Select(resource => resource.Name).ShouldBeEquivalentTo(
            ResourceNames.MigrationService,
            ResourceNames.Api,
            ResourceNames.CatalogApi,
            ResourceNames.AdminIntegrationEventWorker,
            ResourceNames.CatalogIntegrationEventWorker,
            ResourceNames.WebApp,
            ResourceNames.PublicWebApp);
        foreach (var resource in buildResources)
        {
            var image = resource.Annotations.OfType<ContainerImageAnnotation>().ShouldHaveSingleItem();
            image.Registry.ShouldBe(registry);
            image.Tag.ShouldBe(imageTag);
            var configuration = await ExecutionConfigurationBuilder.Create(resource).WithEnvironmentVariablesConfig()
                .BuildAsync(builder.ExecutionContext, NullLogger.Instance, TestContext.Current.CancellationToken);
            configuration.EnvironmentVariables.ShouldContain(KeyValuePair.Create("VT_DEPLOYMENT_VERSION", deploymentVersion));
            configuration.EnvironmentVariables.ShouldContain(KeyValuePair.Create("VT_SOURCE_REVISION", sourceRevision));
        }
    }

    [Fact]
    public void Default_publish_model_excludes_local_developer_resources_even_when_configured()
    {
        // Arrange
        string[] args = ["--operation", "publish", "--publisher", "manifest"];
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = args,
            ProjectDirectory = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost")
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [GrafanaLgtmStackDefaults.EnableObservabilityStackVariable] = "1",
            ["Aspire:Features:DatabaseObservability"] = "true",
            ["Aspire:Features:PostgreSqlServerMetrics"] = "true"
        });

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfileArguments.FromArguments(args));
        var names = builder.Resources.Select(resource => resource.Name).ToArray();

        // Assert
        names.ShouldNotContain(ResourceNames.DatabaseObservability);
        names.ShouldNotContain("pgweb");
        names.ShouldNotContain("redisinsight");
        names.ShouldNotContain(GrafanaLgtmStackDefaults.ResourceNames.Grafana);
        names.ShouldNotContain(ResourceNames.IdentityProvider);
        names.ShouldNotContain(ResourceNames.PostgreSqlServerMetricsUsername);
        names.ShouldNotContain(ResourceNames.PostgreSqlServerMetricsPassword);
        names.ShouldContain(ResourceNames.AdminDatabase);
        names.ShouldContain(ResourceNames.CatalogDatabase);
        names.ShouldContain(ResourceNames.SecurityDatabase);
        names.ShouldContain(ResourceNames.WebApp);
        names.ShouldContain(ResourceNames.PublicWebApp);
    }

    [Fact]
    public async Task Publish_keeps_API_readiness_dependencies_and_leaves_maintenance_to_deployed_services()
    {
        // Arrange
        string[] args = ["--operation", "publish", "--publisher", "manifest"];
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = args,
            ProjectDirectory = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost")
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [HttpMaintenanceConfiguration.EnabledKey] = "true"
        });

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfileArguments.FromArguments(args));
        var web = AppHostTestBuilder.Project(builder, ResourceNames.PublicWebApp);
        var api = AppHostTestBuilder.Project(builder, ResourceNames.CatalogApi);
        var environment = await AppHostModel.EvaluateEnvironment(web, TestContext.Current.CancellationToken);

        // Assert
        environment.Keys.ShouldNotContain(HttpMaintenanceConfiguration.EnabledEnvironmentVariable);
        web.Annotations.OfType<WaitAnnotation>().ShouldHaveSingleItem(wait => wait.Resource == api)
            .WaitType.ShouldBe(WaitType.WaitUntilHealthy);
    }

    [Theory]
    [InlineData(HostedProfile.Full, true, true)]
    [InlineData(HostedProfile.System, true, false)]
    [InlineData(HostedProfile.Admin, false, false)]
    public void Run_profiles_select_their_product_and_developer_resources(
        HostedProfile profile,
        bool includesWebApplications,
        bool includesDeveloperTooling)
    {
        // Arrange
        var builder = AppHostTestBuilder.Create(new Dictionary<string, string?>
        {
            [GrafanaLgtmStackDefaults.EnableObservabilityStackVariable] = "1",
            ["Aspire:Features:DatabaseObservability"] = "true",
            ["Aspire:Features:PostgreSqlServerMetrics"] = "true"
        });

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfileArguments.FromArguments(profile.ToArguments()));
        var names = builder.Resources.Select(resource => resource.Name).ToArray();

        // Assert
        names.ShouldContain(ResourceNames.Api);
        names.ShouldContain(ResourceNames.CatalogApi);
        names.ShouldContain(ResourceNames.AdminIntegrationEventWorker);
        names.ShouldContain(ResourceNames.IdentityProvider);
        names.ShouldContain(ResourceNames.ManagementWebClientSecret);
        names.ShouldContain(GrafanaLgtmStackDefaults.ResourceNames.OpenTelemetryCollector);
        names.Contains(ResourceNames.WebApp, StringComparer.Ordinal).ShouldBe(includesWebApplications);
        names.Contains(ResourceNames.PublicWebApp, StringComparer.Ordinal).ShouldBe(includesWebApplications);
        names.Contains(ResourceNames.CatalogIntegrationEventWorker, StringComparer.Ordinal).ShouldBe(includesWebApplications);
        names.Contains(ResourceNames.SeaweedFs, StringComparer.Ordinal).ShouldBe(includesWebApplications);
        names.Contains(ResourceNames.Cache, StringComparer.Ordinal).ShouldBe(includesWebApplications);
        names.Contains("pgweb", StringComparer.Ordinal).ShouldBe(includesDeveloperTooling);
        names.Contains("redisinsight", StringComparer.Ordinal).ShouldBe(includesDeveloperTooling);
        names.Contains(GrafanaLgtmStackDefaults.ResourceNames.Grafana, StringComparer.Ordinal).ShouldBe(includesDeveloperTooling);
        names.Contains(ResourceNames.DatabaseObservability, StringComparer.Ordinal).ShouldBe(includesDeveloperTooling);
        names.Contains(ResourceNames.PostgreSqlServerMetricsUsername, StringComparer.Ordinal).ShouldBe(includesDeveloperTooling);
    }

    [Fact]
    public void Publish_mode_model_omits_the_local_identity_provider()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest"],
            ProjectDirectory = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost")
        });

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.System);

        // Assert
        builder.ExecutionContext.IsPublishMode.ShouldBeTrue();
        builder.Resources.ShouldNotContain(resource => resource.Name == ResourceNames.IdentityProvider);
        builder.Resources.ShouldNotContain(resource => resource.Name == ResourceNames.IdentityProviderK6ClientSecret);
        builder.Resources.ShouldContain(resource => resource.Name == ResourceNames.PublicWebApp);
    }

    [Fact]
    public async Task Publish_mode_uses_deployment_managed_OIDC_settings_even_with_local_overrides()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--operation", "publish", "--publisher", "manifest"],
            ProjectDirectory = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost")
        });
        builder.Configuration["AppHost:External:IdentityProvider:Authority"] = "http://localhost:8080/realms/local-test";
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.System);
        var managementWeb = builder.Resources.Where(resource => resource.Name == ResourceNames.WebApp).ShouldHaveSingleItem();

        // Act
        var configuration = await ExecutionConfigurationBuilder.Create(managementWeb)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(builder.ExecutionContext, NullLogger.Instance, TestContext.Current.CancellationToken);
        var environment = configuration.EnvironmentVariables.Select(static variable => variable.Key).ToArray();

        // Assert
        builder.Resources.Select(resource => resource.Name).ShouldNotContain(ResourceNames.ManagementWebClientSecret);
        environment.ShouldNotContain("Authentication__ClientId");
        environment.ShouldNotContain("Authentication__ClientSecret");
        environment.ShouldContain("Authentication__TokenExchange__Enabled");
        environment.ShouldContain("Authentication__TokenExchange__Provider");
        environment.ShouldNotContain("Authentication__Authority");
        environment.ShouldNotContain("Authentication__Issuer");
        environment.ShouldNotContain("Authentication__AllowHttpDevelopmentAuthority");
    }

    [Fact]
    public void Run_mode_model_adds_the_local_identity_provider()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            ProjectDirectory = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src", "ViajantesTurismo.AppHost")
        });

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.System);

        // Assert
        builder.ExecutionContext.IsRunMode.ShouldBeTrue();
        builder.Resources.ShouldContain(resource => resource.Name == ResourceNames.IdentityProvider);
    }
}
