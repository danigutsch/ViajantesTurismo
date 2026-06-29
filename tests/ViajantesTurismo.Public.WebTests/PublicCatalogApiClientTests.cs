namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
public sealed class PublicCatalogApiClientTests
{
    [Fact]
    public async Task GetPublishedTours_requests_public_catalog_endpoint_and_skips_null_items()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return PublicCatalogApiClientTestsHelpers.JsonResponse("""
                [
                  {
                    "id":"11111111-1111-1111-1111-111111111111",
                    "adminTourId":"22222222-2222-2222-2222-222222222222",
                    "identifier":"TOUR-1",
                    "title":"First tour",
                    "slug":"first-tour",
                    "isPublished":true,
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
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var tours = await sut.GetPublishedTours(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("/public/catalog/tours", requestPath);
        Assert.Collection(
            tours,
            tour => Assert.Equal("first-tour", tour.Slug),
            tour => Assert.Equal("second-tour", tour.Slug));
    }

    [Theory]
    [InlineData("group tour", "/public/catalog/tours/group%20tour")]
    [InlineData("camino/norte", "/public/catalog/tours/camino%2Fnorte")]
    [InlineData("tour?#fragment", "/public/catalog/tours/tour%3F%23fragment")]
    public async Task GetPublishedTourBySlug_escapes_the_slug_route_segment(string slug, string expectedPath)
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return PublicCatalogApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"11111111-1111-1111-1111-111111111111",
                  "adminTourId":"22222222-2222-2222-2222-222222222222",
                  "identifier":"TOUR-1",
                  "title":"First tour",
                  "slug":"first-tour",
                  "isPublished":true,
                  "images":[],
                  "updatedAt":"2026-06-25T10:00:00+00:00"
                }
                """);
        });
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var tour = await sut.GetPublishedTourBySlug(slug, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(tour);
        Assert.Equal(expectedPath, requestPath);
    }

    [Fact]
    public async Task GetPublishedTourBySlug_returns_null_when_catalog_returns_notfound()
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var tour = await sut.GetPublishedTourBySlug("missing-tour", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(tour);
    }

    [Fact]
    public async Task GetPublishedTourBySlug_throws_when_catalog_returns_unexpected_error()
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var act = sut.GetPublishedTourBySlug("error-tour", TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => await act);
    }

    [Theory]
    [InlineData("home.hero", "pt-BR", "/public/catalog/content/home.hero?culture=pt-BR")]
    [InlineData("home/hero", "en-US", "/public/catalog/content/home/hero?culture=en-US")]
    public async Task GetPublicContent_requests_public_content_endpoint(string key, string culture, string expectedPath)
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.Path + request.QueryString.Value;
            return PublicCatalogApiClientTestsHelpers.JsonResponse("""
                {
                  "language":2,
                  "title":"Bem-vindo",
                  "body":"Pedale conosco",
                  "seoTitle":"Cicloturismo no Brasil",
                  "metaDescription":null,
                  "shareSummary":null,
                  "requiresHumanReview":false
                }
                """);
        });
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var content = await sut.GetPublicContent(key, culture, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(content);
        Assert.Equal(expectedPath, requestPath);
        Assert.Equal("Bem-vindo", content.Title);
    }

    [Fact]
    public async Task GetPublicContent_returns_null_when_catalog_returns_notfound()
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var content = await sut.GetPublicContent("home.hero", "pt-BR", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(content);
    }

}
