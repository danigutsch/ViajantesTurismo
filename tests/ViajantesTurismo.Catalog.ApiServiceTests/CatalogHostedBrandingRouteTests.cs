using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

/// <summary>
/// Verifies that Catalog.ApiService hosts the Branding module behind its own exact-audience scheme.
/// </summary>
[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogHostedBrandingRouteTests
{
    private const string ManagementBrandingPath = "/api/v1/branding/settings";
    private const string PublicBrandingPath = "/api/v1/public/branding";

    [Fact]
    public async Task Branding_token_reads_management_branding_in_the_catalog_host()
    {
        // Arrange
        await using var baseFactory = CatalogApiTestHost.CreateAnonymous();
        await using var factory = CatalogHostedBrandingTestHost.Create(baseFactory);
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureBrandingClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri(ManagementBrandingPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull().IsWeak.ShouldBeFalse();
    }

    [Fact]
    public async Task Catalog_token_cannot_use_branding_routes()
    {
        // Arrange
        await using var baseFactory = CatalogApiTestHost.CreateAnonymous();
        await using var factory = CatalogHostedBrandingTestHost.Create(baseFactory);
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri(ManagementBrandingPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Branding_token_cannot_use_catalog_routes()
    {
        // Arrange
        await using var baseFactory = CatalogApiTestHost.CreateAnonymous();
        await using var factory = CatalogHostedBrandingTestHost.Create(baseFactory);
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureBrandingClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Public_branding_stays_anonymous_in_the_catalog_host()
    {
        // Arrange
        await using var baseFactory = CatalogApiTestHost.CreateAnonymous();
        await using var factory = CatalogHostedBrandingTestHost.Create(baseFactory);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(PublicBrandingPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Every_branding_route_names_a_branding_policy_or_is_anonymous()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is { } route
                && (route.Contains("branding", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        // Act
        var unscoped = endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null
                && !endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                    .Any(data => data.Policy is "branding.read" or "branding.write"))
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();

        // Assert
        endpoints.ShouldNotBeEmpty();
        unscoped.ShouldBeEmpty();
    }
}
