using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogEndpointTests
{
    [Fact]
    public async Task Catalog_tour_list_returns_all_tours()
    {
        // Arrange
        var store = new StubCatalogTourReadModelStore(CatalogEndpointTestsHelpers.CreateTour("TOUR-002", "Dolomites"));
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);
        var tours = await response.Content.ReadFromJsonAsync<CatalogTourDto[]>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tours);
        var tour = Assert.Single(tours);
        Assert.Equal("Dolomites", tour.Title);
        Assert.Equal("TOUR-002", tour.Slug);
        Assert.False(tour.IsPublished);
    }

    [Fact]
    public async Task Public_tour_list_returns_empty_list_when_no_tours_are_published()
    {
        // Arrange
        var store = new StubCatalogTourReadModelStore(CatalogEndpointTestsHelpers.CreateTour("TOUR-002", "Dolomites"));
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var tours = await response.Content.ReadFromJsonAsync<CatalogTourDto[]>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(tours);
        Assert.Empty(tours);
    }

    [Fact]
    public async Task Public_tour_details_returns_notFound_when_tour_is_not_published()
    {
        // Arrange
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(new StubCatalogTourReadModelStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/tours/missing-tour", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Public_tour_details_returns_badRequest_for_whitespace_slug()
    {
        // Arrange
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(new StubCatalogTourReadModelStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/tours/%20", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
