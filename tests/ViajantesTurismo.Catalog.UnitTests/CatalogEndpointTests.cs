using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Catalog.Contracts.Application;

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
        using var response = await client.GetAsync(new Uri("/api/v1/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);
        var tours = await response.Content.ReadFromJsonAsync<CatalogTourDto[]>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        tours.ShouldNotBeNull();
        var tour = tours.ShouldHaveSingleItem();
        tour.Title.ShouldBe("Dolomites");
        tour.Slug.ShouldBe("TOUR-002");
        tour.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task Catalog_tour_details_returns_matching_tour()
    {
        // Arrange
        var tour = CatalogEndpointTestsHelpers.CreateTour("TOUR-002", "Dolomites");
        var store = new StubCatalogTourReadModelStore(tour);
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/catalog/tours/{tour.CatalogTourId}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var dto = await response.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(tour.CatalogTourId);
        dto.Title.ShouldBe("Dolomites");
    }

    [Fact]
    public async Task Catalog_tour_details_returns_notfound_when_tour_is_missing()
    {
        // Arrange
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(new StubCatalogTourReadModelStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/catalog/tours/{Guid.CreateVersion7()}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
            new Uri("/api/v1/public/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var tours = await response.Content.ReadFromJsonAsync<CatalogTourDto[]>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        tours.ShouldNotBeNull();
        tours.ShouldBeEmpty();
    }

    [Fact]
    public async Task Public_tour_details_returns_notfound_when_tour_is_not_published()
    {
        // Arrange
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(new StubCatalogTourReadModelStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/tours/missing-tour", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Public_tour_details_returns_badrequest_for_whitespace_slug()
    {
        // Arrange
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(new StubCatalogTourReadModelStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/tours/%20", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Public_tour_details_trims_slug_before_lookup()
    {
        // Arrange
        var tour = CatalogEndpointTestsHelpers.CreateTour("published-tour", "Published") with { IsPublished = true };
        var store = new StubCatalogTourReadModelStore(tour);
        await using var factory = CatalogEndpointTestsHelpers.CreateFactory(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/tours/%20published-tour%20", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var dto = await response.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        dto.ShouldNotBeNull();
        dto.Slug.ShouldBe("published-tour");
    }
}
