using Microsoft.AspNetCore.TestHost;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Gallery;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Gallery;

/// <summary>Verifies access to the Catalog Gallery placement operations lookup.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[TestHost(TestHost.TestServer)]
public sealed class CatalogGalleryOperationsAuthorizationTests
{
    private const string Path = "/api/v1/catalog/operations/gallery";

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    public async Task Gallery_retry_health_requires_admin_operations_read(string? role, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubCatalogGalleryOperationsQuery(null);
        await using var baseline = CatalogApiTestHost.CreateAnonymous();
        await using var factory = baseline.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<ICatalogGalleryOperationsQuery>(query)));
        using var client = factory.CreateClient();
        if (role is not null)
        {
            CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        var route = new Uri($"{Path}/retry-health", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.RetryHealthRequest.ShouldBeNull();
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    public async Task Gallery_placement_lookup_requires_the_admin_role(string? role, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubCatalogGalleryOperationsQuery(null);
        await using var baseline = CatalogApiTestHost.CreateAnonymous();
        await using var factory = baseline.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<ICatalogGalleryOperationsQuery>(query)));
        using var client = factory.CreateClient();
        if (role is not null)
        {
            CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        var route = new Uri($"{Path}/{Guid.CreateVersion7():D}/placements/1", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Gallery_placement_lookup_rejects_admin_tokens_for_the_branding_audience()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubCatalogGalleryOperationsQuery(null);
        await using var baseline = CatalogApiTestHost.CreateAnonymous();
        await using var factory = baseline.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<ICatalogGalleryOperationsQuery>(query)));
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureBrandingClient(client, "Admin");
        var route = new Uri($"{Path}/{Guid.CreateVersion7():D}/placements/1", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task Gallery_placement_lookup_rejects_invalid_keys_before_querying(bool emptyTour, long sequence)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubCatalogGalleryOperationsQuery(null);
        await using var baseline = CatalogApiTestHost.CreateAnonymous();
        await using var factory = baseline.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<ICatalogGalleryOperationsQuery>(query)));
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var tourId = emptyTour ? Guid.Empty : Guid.CreateVersion7();
        var route = new Uri(FormattableString.Invariant($"{Path}/{tourId:D}/placements/{sequence}"), UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBeNull();
    }
}
