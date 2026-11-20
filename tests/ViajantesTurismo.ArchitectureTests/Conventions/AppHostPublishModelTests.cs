using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Verifies the publish-mode AppHost model without starting application processes or containers.
/// </summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class AppHostPublishModelTests
{
    [Fact]
    public void Default_publish_model_excludes_local_developer_resources_even_when_configured()
    {
        // Arrange
        string[] args = ["--operation", "publish", "--publisher", "manifest"];
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = args,
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost")
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
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost")
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
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost")
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
            ProjectDirectory = Path.Combine(AppHostOrchestrationTestsHelpers.GetRepositoryRoot(), "src", "ViajantesTurismo.AppHost")
        });

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, HostedProfile.System);

        // Assert
        builder.ExecutionContext.IsRunMode.ShouldBeTrue();
        builder.Resources.ShouldContain(resource => resource.Name == ResourceNames.IdentityProvider);
    }
}
