using Aspire.Hosting.ApplicationModel;
using PandaBear.IntegrationTesting;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Verifies that configured external infrastructure replaces the AppHost containers.</summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.AppHostArea)]
public sealed class ExternalInfrastructureTests
{
    private static readonly Dictionary<string, string?> Everything = new(StringComparer.Ordinal)
    {
        [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = "Host=localhost;Database=admin;Username=postgres",
        [$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = "Host=localhost;Database=catalog;Username=postgres",
        [$"ConnectionStrings:{ResourceNames.SecurityDatabase}"] = "Host=localhost;Database=security;Username=postgres",
        [$"ConnectionStrings:{ResourceNames.Cache}"] = "localhost:6379",
        [ExternalInfrastructure.IdentityProviderAuthorityKey] = "http://localhost:8080/realms/test",
        [ExternalInfrastructure.ClamAvHostKey] = "localhost",
        [ExternalInfrastructure.ClamAvPortKey] = "3310",
        [ExternalInfrastructure.SeaweedFsEndpointKey] = "http://localhost:8333",
        [ExternalInfrastructure.SeaweedFsAccessKeyKey] = "synthetic-access",
        [ExternalInfrastructure.SeaweedFsSecretKeyKey] = "synthetic-secret",
        [ExternalInfrastructure.SeaweedFsBucketKey] = "synthetic-media",
        [ExternalInfrastructure.OtlpEndpointKey] = "http://localhost:4317",
    };

    [Theory]
    [InlineData(HostedProfile.Admin)]
    [InlineData(HostedProfile.System)]
    [InlineData(HostedProfile.Full)]
    public void Configured_infrastructure_starts_no_container(HostedProfile profile)
    {
        // Arrange
        var builder = AppHostTestBuilder.Create(Everything);

        // Act
        builder.AddProductResources(profile);

        // Assert
        builder.Resources.OfType<ContainerResource>().ShouldBeEmpty();
        AppHostTestBuilder.Project(builder, ResourceNames.Api).ShouldNotBeNull();
        AppHostTestBuilder.Project(builder, ResourceNames.MigrationService).ShouldNotBeNull();
        AppHostTestBuilder.Project(builder, ResourceNames.AdminIntegrationEventWorker).ShouldNotBeNull();
    }

    [Fact]
    public async Task Projects_receive_the_configured_addresses()
    {
        // Arrange
        var builder = AppHostTestBuilder.Create(Everything);
        builder.AddProductResources(HostedProfile.System);

        // Act
        var api = await AppHostModel.EvaluateEnvironment(AppHostTestBuilder.Project(builder, ResourceNames.Api), TestContext.Current.CancellationToken);
        var catalogWorker = await AppHostModel.EvaluateEnvironment(
            AppHostTestBuilder.Project(builder, ResourceNames.CatalogIntegrationEventWorker),
            TestContext.Current.CancellationToken);
        var web = await AppHostModel.EvaluateEnvironment(AppHostTestBuilder.Project(builder, ResourceNames.WebApp), TestContext.Current.CancellationToken);

        // Assert
        api["Authentication__Authority"].ShouldBe("http://localhost:8080/realms/test");
        api["Authentication__Issuer"].ShouldBe("http://localhost:8080/realms/test");
        api["Authentication__AllowHttpDevelopmentAuthority"].ShouldBe("true");
        api["MalwareScanning__ClamAv__Host"].ShouldBe("localhost");
        api["MalwareScanning__ClamAv__Port"].ShouldBe("3310");
        AppHostModel.ConnectionResource(api[$"ConnectionStrings__{ResourceNames.AdminDatabase}"]).Name.ShouldBe(ResourceNames.AdminDatabase);
        catalogWorker["Catalog__MediaObjectStorage__SeaweedFs__Endpoint"].ShouldBe("http://localhost:8333");
        catalogWorker["Catalog__MediaObjectStorage__SeaweedFs__Bucket"].ShouldBe("synthetic-media");
        AppHostModel.ConnectionResource(web[$"ConnectionStrings__{ResourceNames.Cache}"]).Name.ShouldBe(ResourceNames.Cache);
        AppHostModel.WaitedFor(AppHostTestBuilder.Project(builder, ResourceNames.Api)).ShouldNotContain(ResourceNames.IdentityProvider);
    }

    [Fact]
    public void Media_storage_of_the_admin_profile_comes_from_configuration_only()
    {
        // Arrange
        var builder = AppHostTestBuilder.Create();

        // Act
        builder.AddProductResources(HostedProfile.Admin);

        // Assert
        builder.Resources.OfType<SeaweedFsResource>().ShouldBeEmpty();
        builder.Resources.OfType<PostgresServerResource>().ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData($"ConnectionStrings:{ResourceNames.AdminDatabase}", "Host=localhost")]
    [InlineData(ExternalInfrastructure.ClamAvHostKey, "localhost")]
    [InlineData(ExternalInfrastructure.SeaweedFsEndpointKey, "http://localhost:8333")]
    [InlineData(ExternalInfrastructure.IdentityProviderAuthorityKey, "not-a-uri")]
    [InlineData(ExternalInfrastructure.OtlpEndpointKey, "ftp://localhost")]
    public void Incomplete_or_invalid_external_settings_fail_during_model_creation(string key, string value)
    {
        // Arrange
        var builder = AppHostTestBuilder.Create(new Dictionary<string, string?>(StringComparer.Ordinal) { [key] = value });

        // Act
        Action createModel = () => builder.AddProductResources(HostedProfile.System);

        // Assert
        createModel.ShouldThrow<InvalidOperationException>().Message.ShouldContain(key, StringComparison.Ordinal);
    }
}
