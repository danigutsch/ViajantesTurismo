using System.Text;
using Microsoft.AspNetCore.TestHost;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.Application.PublicContent;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.DependencyInjectionCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogEndpointDependencyCompositionTests
{
    [Fact]
    public async Task Mapped_catalog_mutation_endpoint_dependencies_resolve_from_the_composed_host()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateProductionComposition();

        // Assert
        CatalogApiTestHost.VerifyMappedMutationDependencies(factory);
    }

    [Fact]
    public async Task Failing_public_content_upsert_service_is_activated_by_the_public_content_endpoint()
    {
        // Arrange
        await using var baseFactory = CatalogApiTestHost.Create();
        await using var factory = baseFactory.WithWebHostBuilder(
            builder => builder.ConfigureTestServices(services =>
                services.Replace(ServiceDescriptor.Scoped<PublicContentUpsertService>(
                    _ => throw new InvalidOperationException("Expected endpoint dependency activation.")))));
        using var client = factory.CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }
}
