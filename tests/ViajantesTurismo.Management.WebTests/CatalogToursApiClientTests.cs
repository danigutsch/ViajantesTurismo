using Microsoft.AspNetCore.Http;

namespace ViajantesTurismo.Management.WebTests;

public sealed class CatalogToursApiClientTests
{
    [Fact]
    public async Task GetTours_requests_management_catalog_endpoint_and_skips_null_items()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                [
                  {
                    "id":"11111111-1111-1111-1111-111111111111",
                    "adminTourId":"22222222-2222-2222-2222-222222222222",
                    "identifier":"TOUR-1",
                    "title":"First tour",
                    "slug":"first-tour",
                    "isPublished":false,
                    "images":[],
                    "updatedAt":"2026-06-25T10:00:00+00:00"
                  },
                  null,
                  {
                    "id":"33333333-3333-3333-3333-333333333333",
                    "adminTourId":"44444444-4444-4444-4444-444444444444",
                    "identifier":"TOUR-2",
                    "title":"Second tour",
                    "slug":"second-tour",
                    "isPublished":true,
                    "images":[],
                    "updatedAt":"2026-06-25T11:00:00+00:00"
                  }
                ]
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var tours = await sut.GetTours(Xunit.TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/api/v1/catalog/tours");
        tours.Length.ShouldBe(2);
        tours[0].Slug.ShouldBe("first-tour");
        tours[1].Slug.ShouldBe("second-tour");
    }

    [Fact]
    public async Task GetTours_returns_empty_array_when_catalog_returns_only_nulls()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("[null,null]"));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var tours = await sut.GetTours(Xunit.TestContext.Current.CancellationToken);

        // Assert
        tours.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdatePresentation_sends_management_catalog_presentation_request()
    {
        // Arrange
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            requestMethod = request.Method;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"11111111-1111-1111-1111-111111111111",
                  "adminTourId":"22222222-2222-2222-2222-222222222222",
                  "identifier":"TOUR-1",
                  "title":"Updated tour",
                  "slug":"updated-tour",
                  "isPublished":true,
                  "images":[],
                  "updatedAt":"2026-06-25T10:00:00+00:00"
                }
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var updated = await sut.UpdatePresentation(
            tourId,
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Updated tour",
                Slug = "updated-tour",
                IsPublished = true
            },
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        updated.ShouldNotBeNull();
        requestMethod.ShouldBe(HttpMethods.Put);
        requestPath.ShouldBe("/api/v1/catalog/tours/11111111-1111-1111-1111-111111111111/presentation");
        updated.IsPublished.ShouldBeTrue();
    }

    [Fact]
    public async Task GetTour_requests_management_catalog_details_endpoint()
    {
        // Arrange
        var requestPath = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"11111111-1111-1111-1111-111111111111",
                  "adminTourId":"22222222-2222-2222-2222-222222222222",
                  "identifier":"TOUR-1",
                  "title":"Catalog tour",
                  "slug":"catalog-tour",
                  "isPublished":false,
                  "images":[],
                  "updatedAt":"2026-06-25T10:00:00+00:00"
                }
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var tour = await sut.GetTour(tourId, Xunit.TestContext.Current.CancellationToken);

        // Assert
        tour.ShouldNotBeNull();
        requestPath.ShouldBe("/api/v1/catalog/tours/11111111-1111-1111-1111-111111111111");
        tour.Slug.ShouldBe("catalog-tour");
    }

    [Fact]
    public async Task GetTour_returns_null_when_catalog_returns_notfound()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var tour = await sut.GetTour(Guid.CreateVersion7(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        tour.ShouldBeNull();
    }

    [Fact]
    public async Task GetTour_throws_when_catalog_returns_success_with_null_body()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("null"));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        Func<Task> act = async () => await sut.GetTour(Guid.CreateVersion7(), Xunit.TestContext.Current.CancellationToken);

        // Assert
        var exception = await act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The catalog tour response body was empty.");
    }

    [Fact]
    public async Task UpdatePresentation_returns_null_when_catalog_returns_notfound()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var updated = await sut.UpdatePresentation(
            Guid.CreateVersion7(),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Missing",
                Slug = "missing",
                IsPublished = true
            },
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        updated.ShouldBeNull();
    }

    [Fact]
    public async Task UpdatePresentation_throws_when_catalog_returns_success_with_null_body()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("null"));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        Func<Task> act = async () => await sut.UpdatePresentation(
            Guid.CreateVersion7(),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Missing",
                Slug = "missing",
                IsPublished = true
            },
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        var exception = await act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The catalog tour response body was empty.");
    }

}
