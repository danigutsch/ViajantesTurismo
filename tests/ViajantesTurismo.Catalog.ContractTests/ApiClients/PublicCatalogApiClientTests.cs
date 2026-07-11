using System.Net;
using System.Text.Json;
using PublicCatalogApiClientTestsHelpers = SharedKernel.Testing.Contracts.ContractHttpClientTestHelper;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Catalog.ContractTests.ApiClients;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class PublicCatalogApiClientTests
{
    [Fact]
    public async Task GetPublishedTours_requests_public_catalog_endpoint_and_skips_null_items()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
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
        requestPath.ShouldBe("/api/v1/public/catalog/tours");
        tours.Length.ShouldBe(2);
        tours[0].Slug.ShouldBe("first-tour");
        tours[1].Slug.ShouldBe("second-tour");
    }

    [Fact]
    public async Task GetPublishedTours_deserializes_public_media_images_and_variants()
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => PublicCatalogApiClientTestsHelpers.JsonResponse("""
            [
              {
                "id":"11111111-1111-1111-1111-111111111111",
                "adminTourId":"22222222-2222-2222-2222-222222222222",
                "identifier":"TOUR-1",
                "title":"First tour",
                "slug":"first-tour",
                "isPublished":true,
                "images":[
                  {
                    "uri":"https://cdn.example/cover.jpg",
                    "altText":"Cover image",
                    "caption":"Mountain pass",
                    "sortOrder":1,
                    "isCover":true,
                    "responsiveVariants":[
                      {"uri":"https://cdn.example/cover-320.jpg","width":320,"height":213,"contentType":"image/jpeg","fileSizeBytes":512}
                    ]
                  }
                ],
                "updatedAt":"2026-06-25T10:00:00+00:00"
              }
            ]
            """));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var tours = await sut.GetPublishedTours(TestContext.Current.CancellationToken);

        // Assert
        var tour = tours.ShouldHaveSingleItem();
        var image = tour.Images.ShouldHaveSingleItem();
        image.IsCover.ShouldBeTrue();
        image.Uri.ToString().ShouldBe("https://cdn.example/cover.jpg");
        image.Caption.ShouldBe("Mountain pass");
        var variant = image.ResponsiveVariants.ShouldHaveSingleItem();
        variant.Width.ShouldBe(320);
        variant.Uri.ToString().ShouldBe("https://cdn.example/cover-320.jpg");
    }

    [Theory]
    [InlineData("group tour", "/api/v1/public/catalog/tours/group%20tour")]
    [InlineData("camino/norte", "/api/v1/public/catalog/tours/camino%2Fnorte")]
    [InlineData("tour?#fragment", "/api/v1/public/catalog/tours/tour%3F%23fragment")]
    public async Task GetPublishedTourBySlug_escapes_the_slug_route_segment(string slug, string expectedPath)
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
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
        tour.ShouldNotBeNull();
        requestPath.ShouldBe(expectedPath);
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
        tour.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPublishedTourBySlug_rejects_success_with_empty_or_null_body(bool jsonNull)
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => jsonNull
            ? PublicCatalogApiClientTestsHelpers.JsonResponse("null")
            : new HttpResponseMessage(HttpStatusCode.OK));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        Func<Task> act = async () => await sut.GetPublishedTourBySlug("missing-body", TestContext.Current.CancellationToken);

        // Assert
        if (jsonNull)
        {
            var exception = await act.ShouldThrow<InvalidOperationException>();
            exception.Message.ShouldBe("The published tour response body was empty.");
            return;
        }

        await act.ShouldThrow<JsonException>();
    }

    [Fact]
    public async Task GetPublishedTourBySlug_throws_when_catalog_returns_unexpected_error()
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        Func<Task> act = async () => await sut.GetPublishedTourBySlug("error-tour", TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrow<HttpRequestException>();
    }

    [Theory]
    [InlineData("home.hero", "pt-BR", "/api/v1/public/catalog/content/home.hero?culture=pt-BR")]
    [InlineData("home/hero", "en-US", "/api/v1/public/catalog/content/home/hero?culture=en-US")]
    [InlineData("/home//hero/", "en-US", "/api/v1/public/catalog/content/home/hero?culture=en-US")]
    [InlineData("home / hero", "en-US", "/api/v1/public/catalog/content/home/hero?culture=en-US")]
    public async Task GetPublicContent_requests_public_content_endpoint(string key, string culture, string expectedPath)
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
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
        content.ShouldNotBeNull();
        requestPath.ShouldBe(expectedPath);
        content.Title.ShouldBe("Bem-vindo");
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
        content.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPublicContent_rejects_success_with_empty_or_null_body(bool jsonNull)
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => jsonNull
            ? PublicCatalogApiClientTestsHelpers.JsonResponse("null")
            : new HttpResponseMessage(HttpStatusCode.OK));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        Func<Task> act = async () => await sut.GetPublicContent("home.hero", "en-US", TestContext.Current.CancellationToken);

        // Assert
        if (jsonNull)
        {
            var exception = await act.ShouldThrow<InvalidOperationException>();
            exception.Message.ShouldBe("The public content response body was empty.");
            return;
        }

        await act.ShouldThrow<JsonException>();
    }
}
