using Aspire.Hosting.ApplicationModel;
using PandaBear.IntegrationTesting;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Verifies the AppHost wiring from its application model, without starting anything.</summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class AppHostModelTests
{
    [Fact]
    public async Task Catalog_API_uses_both_databases_after_the_migrations()
    {
        // Arrange
        var builder = AppHostTestBuilder.Create();

        // Act
        builder.AddProductResources(HostedProfile.System);
        var catalogApi = AppHostTestBuilder.Project(builder, ResourceNames.CatalogApi);
        var environment = await AppHostModel.EvaluateEnvironment(catalogApi, TestContext.Current.CancellationToken);

        // Assert
        AppHostModel.WaitedFor(catalogApi).ShouldContain(ResourceNames.AdminDatabase);
        AppHostModel.WaitedFor(catalogApi).ShouldContain(ResourceNames.CatalogDatabase);
        AppHostModel.WaitedFor(catalogApi).ShouldContain(ResourceNames.MigrationService);
        AppHostModel.ConnectionResource(environment[$"ConnectionStrings__{ResourceNames.CatalogDatabase}"]).Name.ShouldBe(ResourceNames.CatalogDatabase);
    }

    [Fact]
    public async Task Management_web_uses_the_security_database_after_the_migrations()
    {
        // Arrange
        var builder = AppHostTestBuilder.Create();

        // Act
        builder.AddProductResources(HostedProfile.System);
        var web = AppHostTestBuilder.Project(builder, ResourceNames.WebApp);
        var environment = await AppHostModel.EvaluateEnvironment(web, TestContext.Current.CancellationToken);

        // Assert
        AppHostModel.WaitedFor(web).ShouldContain(ResourceNames.SecurityDatabase);
        AppHostModel.WaitedFor(web).ShouldContain(ResourceNames.MigrationService);
        AppHostModel.ConnectionResource(environment[$"ConnectionStrings__{ResourceNames.SecurityDatabase}"]).Name.ShouldBe(ResourceNames.SecurityDatabase);
    }

    [Fact]
    public async Task Database_observability_reads_its_own_connection_strings_after_the_migrations()
    {
        // Arrange
        var builder = AppHostTestBuilder.Create(new Dictionary<string, string?> { ["Aspire:Features:DatabaseObservability"] = "true" });

        // Act
        builder.AddProductResources(HostedProfile.Full);
        var observability = AppHostTestBuilder.Project(builder, ResourceNames.DatabaseObservability);
        var environment = await AppHostModel.EvaluateEnvironment(observability, TestContext.Current.CancellationToken);

        // Assert
        AppHostModel.WaitedFor(observability).ShouldContain(ResourceNames.AdminDatabase);
        AppHostModel.WaitedFor(observability).ShouldContain(ResourceNames.CatalogDatabase);
        AppHostModel.WaitedFor(observability).ShouldContain(ResourceNames.MigrationService);
        environment.Keys.ShouldContain("ConnectionStrings__admin-index-health");
        environment.Keys.ShouldContain("ConnectionStrings__catalog-index-health");
        environment.Keys.ShouldNotContain($"ConnectionStrings__{ResourceNames.AdminDatabase}");
        environment.Keys.ShouldNotContain($"ConnectionStrings__{ResourceNames.CatalogDatabase}");
    }

    [Theory]
    [InlineData(ResourceNames.Api)]
    [InlineData(ResourceNames.CatalogApi)]
    [InlineData(ResourceNames.WebApp)]
    [InlineData(ResourceNames.PublicWebApp)]
    public async Task HTTP_projects_run_in_the_apphost_environment_with_a_health_check(string name)
    {
        // Arrange
        var builder = AppHostTestBuilder.Create();

        // Act
        builder.AddProductResources(HostedProfile.System);
        var project = AppHostTestBuilder.Project(builder, name);
        var environment = await AppHostModel.EvaluateEnvironment(project, TestContext.Current.CancellationToken);

        // Assert
        AppHostModel.HasHttpHealthCheck(project, "/health").ShouldBeTrue();
        environment["ASPNETCORE_ENVIRONMENT"].ShouldBe(builder.Environment.EnvironmentName);
    }

    [Theory]
    [InlineData(ResourceNames.WebApp, ResourceNames.Api)]
    [InlineData(ResourceNames.WebApp, ResourceNames.CatalogApi)]
    [InlineData(ResourceNames.PublicWebApp, ResourceNames.CatalogApi)]
    public async Task Local_maintenance_starts_the_web_page_while_its_API_is_unready(string webName, string apiName)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var builder = AppHostTestBuilder.Create(new Dictionary<string, string?>
        {
            [HttpMaintenanceConfiguration.EnabledKey] = "true"
        });

        // Act
        builder.AddProductResources(HostedProfile.System);
        var web = AppHostTestBuilder.Project(builder, webName);
        var api = AppHostTestBuilder.Project(builder, apiName);
        var migrations = AppHostTestBuilder.Project(builder, ResourceNames.MigrationService);
        var webEnvironment = await AppHostModel.EvaluateEnvironment(web, ct);
        var apiEnvironment = await AppHostModel.EvaluateEnvironment(api, ct);
        var migrationEnvironment = await AppHostModel.EvaluateEnvironment(migrations, ct);

        // Assert
        webEnvironment[HttpMaintenanceConfiguration.EnabledEnvironmentVariable].ShouldBe("true");
        apiEnvironment[HttpMaintenanceConfiguration.EnabledEnvironmentVariable].ShouldBe("true");
        migrationEnvironment.Keys.ShouldNotContain(HttpMaintenanceConfiguration.EnabledEnvironmentVariable);
        web.Annotations.OfType<WaitAnnotation>().ShouldHaveSingleItem(wait => wait.Resource == api)
            .WaitType.ShouldBe(WaitType.WaitUntilStarted);
        AppHostModel.HasHttpHealthCheck(api, "/health").ShouldBeTrue();
        AppHostModel.WaitedFor(api).ShouldContain(ResourceNames.MigrationService);
    }

    [Fact]
    public async Task Public_web_builds_its_sitemap_from_its_own_https_endpoint()
    {
        // Arrange
        var builder = AppHostTestBuilder.Create();

        // Act
        builder.AddProductResources(HostedProfile.System);
        var publicWeb = AppHostTestBuilder.Project(builder, ResourceNames.PublicWebApp);
        var environment = await AppHostModel.EvaluateEnvironment(publicWeb, TestContext.Current.CancellationToken);

        // Assert
        var origin = environment["PublicWeb__Sitemap__CanonicalOrigin"].ShouldBeOfType<EndpointReference>();
        origin.Resource.ShouldBe(publicWeb);
        origin.EndpointName.ShouldBe("https");
    }

    [Fact]
    public void Local_identity_provider_uses_an_explicit_registry_and_a_pinned_image()
    {
        // Arrange
        var builder = AppHostTestBuilder.Create();

        // Act
        builder.AddProductResources(HostedProfile.System);
        var identityProvider = builder.Resources.OfType<ContainerResource>().ShouldHaveSingleItem(resource => resource.Name == ResourceNames.IdentityProvider);
        var image = identityProvider.Annotations.OfType<ContainerImageAnnotation>().ShouldHaveSingleItem();

        // Assert
        image.Registry.ShouldBe("quay.io");
        image.Image.ShouldBe("keycloak/keycloak");
        image.SHA256.ShouldBe("2eb3cd316835c990e69e26ade292ffa78f6fb0db7d5fc6377463c162e1979ac0");
    }

    [Fact]
    public void Local_secrets_are_generated_secret_parameters()
    {
        // Arrange
        var builder = AppHostTestBuilder.Create();

        // Act
        builder.AddProductResources(HostedProfile.System);
        var secrets = builder.Resources.OfType<ParameterResource>()
            .Where(parameter => parameter.Name is ResourceNames.ManagementWebClientSecret or ResourceNames.IdentityProviderAdminPassword)
            .Select(parameter => (parameter.Secret, Generated: parameter.Default is GenerateParameterDefault))
            .ToArray();

        // Assert
        secrets.ShouldBe([(true, true), (true, true)]);
    }
}
