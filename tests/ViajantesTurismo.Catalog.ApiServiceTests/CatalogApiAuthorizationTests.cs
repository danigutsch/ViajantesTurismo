using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

/// <summary>
/// Verifies the authorization boundary between management and public Catalog endpoints.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiAuthorizationTests
{
    [Fact]
    public async Task Management_catalog_endpoint_rejects_anonymous_requests()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_catalog_endpoint_rejects_a_token_from_an_untrusted_issuer()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureClientWithUntrustedIssuer(client);

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_catalog_endpoint_rejects_a_token_for_another_audience()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureClientWithWrongAudience(client);

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Management_catalog_endpoint_rejects_an_authenticated_role_without_permissions()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Guest");

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
