using PandaBear.Testing;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class CatalogApiMaintenanceTests
{
    [Fact]
    public async Task Maintenance_replaces_an_ordinary_catalog_response_with_an_uncacheable_problem()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous(configuration =>
            configuration[HttpMaintenanceConfiguration.EnabledKey] = "true");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/robots.txt", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
    }
}
