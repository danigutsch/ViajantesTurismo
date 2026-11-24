using Aspire.Hosting.ApplicationModel;
using PandaBear.IntegrationTesting;
using PandaBear.Testing;
using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

[Trait(TestTraitNames.CategoryName, TestTraits.DocumentationCategory)]
public sealed class LocalRuntimeDocumentationTests
{
    [Theory]
    [InlineData(ResourceNames.Api, "adminApi[Admin.Api internal HTTP]", false)]
    [InlineData(ResourceNames.CatalogApi, "catalogApi[Catalog.Api internal HTTP]", false)]
    [InlineData(ResourceNames.WebApp, "ManagementWeb[Management.Web external HTTP]", true)]
    [InlineData(ResourceNames.PublicWebApp, "PublicWeb[Public.Web external HTTP]", true)]
    public void The_local_runtime_graph_describes_the_model_endpoint_exposure(
        string name, string label, bool external)
    {
        // Arrange
        var builder = AppHostTestBuilder.Create();
        builder.AddProductResources(HostedProfile.System);
        var repository = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var document = File.ReadAllText(Path.Combine(repository, "docs", "architecture", "runtime-wiring-and-deployment.md"));

        // Act
        var project = AppHostTestBuilder.Project(builder, name);
        var endpoints = project.Annotations.OfType<EndpointAnnotation>().ToArray();

        // Assert
        endpoints.ShouldNotBeEmpty();
        endpoints.Any(endpoint => endpoint.IsExternal).ShouldBe(external);
        document.ShouldContain(label, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AdminPerformanceSmoke", "VT_ASPIRE_ENABLE_PERFORMANCE_TESTS")]
    [InlineData("ObservabilityStack", "ASPIRE_ENABLE_OBSERVABILITY_STACK")]
    [InlineData("DatabaseObservability", "Aspire:Features:DatabaseObservability")]
    public void The_local_runtime_graph_records_resource_enablement(string graphNode, string switchName)
    {
        // Arrange
        var repository = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var documentPath = Path.Combine(repository, "docs", "architecture", "runtime-wiring-and-deployment.md");

        // Act
        var graphLine = File.ReadLines(documentPath).ShouldHaveSingleItem(line =>
            line.StartsWith($"    {graphNode}[", StringComparison.Ordinal));

        // Assert
        graphLine.ShouldContain(switchName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_runtime_view_distinguishes_service_discovery_from_migration_readiness()
    {
        // Arrange
        var builder = AppHostTestBuilder.Create();
        builder.AddProductResources(HostedProfile.System);
        var web = AppHostTestBuilder.Project(builder, ResourceNames.WebApp);
        var repository = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var cancellationToken = TestContext.Current.CancellationToken;
        var document = await File.ReadAllTextAsync(
            Path.Combine(repository, "docs", "architecture", "runtime-wiring-and-deployment.md"), cancellationToken);

        // Act
        var waits = web.Annotations.OfType<WaitAnnotation>().ToArray();
        var environment = await AppHostModel.EvaluateEnvironment(web, cancellationToken);
        var adminApiReferencePrefix = $"services__{ResourceNames.Api}__";
        var referencesAdminApi = environment.Keys.Any(key => key.StartsWith(adminApiReferencePrefix, StringComparison.Ordinal));

        // Assert
        waits.ShouldHaveSingleItem(wait => wait.Resource.Name == ResourceNames.MigrationService).WaitType.ShouldBe(WaitType.WaitForCompletion);
        referencesAdminApi.ShouldBeTrue();
        document.ShouldContain("web -->|service discovery| admin", StringComparison.Ordinal);
        document.ShouldContain("web -->|waits for completion| migration", StringComparison.Ordinal);
    }
}
