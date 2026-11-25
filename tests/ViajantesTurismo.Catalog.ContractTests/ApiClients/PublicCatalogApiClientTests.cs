using System.Net;
using SharedKernel.HttpClients;
using PublicCatalogApiClientTestsHelpers = SharedKernel.Testing.Contracts.ContractHttpClientTestHelper;
using SharedKernel.Testing.Contracts;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Catalog.ContractTests.ApiClients;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class PublicCatalogApiClientTests
{
    [Fact]
    public async Task GetPublishedTourPage_requests_the_selected_public_catalog_page()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return PublicCatalogApiClientTestsHelpers.JsonResponse("""
                {"page":2,"pageSize":2,"total":4,"tours":[
                  {
                    "title":"First tour",
                    "slug":"first-tour",
                    "summary":"First tour summary",
                    "images":[],
                    "updatedAt":"2026-06-25T10:00:00+00:00"
                  },
                  {
                    "title":"Second tour",
                    "slug":"second-tour",
                    "summary":"Second tour summary",
                    "images":[],
                    "updatedAt":"2026-06-25T11:00:00+00:00"
                  }
                ]}
                """);
        });
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var outcome = await sut.GetPublishedTourPage("2", 2, TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/api/v1/public/catalog/tours/browse/page?pageSize=2&page=2");
        var page = outcome.Value.ShouldNotBeNull();
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(2);
        page.Total.ShouldBe(4);
        var tours = page.Tours;
        tours.Count.ShouldBe(2);
        tours[0].Slug.ShouldBe("first-tour");
        tours[1].Slug.ShouldBe("second-tour");
        tours[0].Links.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPublishedTourPage_deserializes_public_media_images_and_variants()
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => PublicCatalogApiClientTestsHelpers.JsonResponse("""
            {"page":1,"pageSize":12,"total":1,"tours":[
              {
                "title":"First tour",
                "slug":"first-tour",
                "summary":"First tour summary",
                "links":[{"rel":"self","href":"/api/v1/public/catalog/tours/first-tour","method":"GET"}],
                "images":[
                  {
                    "id":"55555555-5555-5555-5555-555555555555",
                    "altText":"Cover image",
                    "caption":"Mountain pass",
                    "sortOrder":1,
                    "isCover":true,
                    "isDecorative":false,
                    "responsiveVariants":[
                      {"width":320,"height":213,"contentType":"image/jpeg","fileSizeBytes":512}
                    ]
                  }
                ],
                "updatedAt":"2026-06-25T10:00:00+00:00"
              }
            ]}
            """));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var outcome = await sut.GetPublishedTourPage(page: null, 12, TestContext.Current.CancellationToken);

        // Assert
        var tours = outcome.Value.ShouldNotBeNull().Tours;
        var tour = tours.ShouldHaveSingleItem();
        var self = tour.Links.ShouldHaveSingleItem();
        self.Rel.ShouldBe("self");
        self.Href.ShouldBe("/api/v1/public/catalog/tours/first-tour");
        self.Method.ShouldBe("GET");
        var image = tour.Images.ShouldHaveSingleItem();
        image.IsCover.ShouldBeTrue();
        image.Id.ShouldBe(Guid.Parse("55555555-5555-5555-5555-555555555555"));
        image.Caption.ShouldBe("Mountain pass");
        var variant = image.ResponsiveVariants.ShouldHaveSingleItem();
        variant.Width.ShouldBe(320);
        variant.ContentType.ShouldBe("image/jpeg");
    }

    [Fact]
    public async Task GetPublicMedia_requests_the_exact_rendition_endpoint()
    {
        // Arrange
        var requestPath = string.Empty;
        var imageId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("image"u8.ToArray())
            };
        });
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var media = await sut.GetPublicMedia(imageId, 640, "jpg", TestContext.Current.CancellationToken);

        // Assert
        await using var response = media.ShouldNotBeNull();
        requestPath.ShouldBe("/api/v1/public/catalog/media/55555555-5555-5555-5555-555555555555/640/jpg");
    }

    [Fact]
    public async Task GetPublicMedia_returns_null_and_disposes_the_not_found_response()
    {
        // Arrange
        var content = new TrackingHttpContent();
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = content
        };
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => response);
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var media = await sut.GetPublicMedia(Guid.CreateVersion7(), 640, "jpg", TestContext.Current.CancellationToken);

        // Assert
        media.ShouldBeNull();
        content.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPublicMedia_disposes_the_response_when_the_upstream_status_is_unsuccessful()
    {
        // Arrange
        var content = new TrackingHttpContent();
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = content
        };
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => response);
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        Func<Task> act = async () => await sut.GetPublicMedia(Guid.CreateVersion7(), 640, "jpg", TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrow<HttpRequestException>();
        content.IsDisposed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("group tour", "/api/v1/public/catalog/tours/group%20tour")]
    [InlineData("camino/norte", "/api/v1/public/catalog/tours/camino%2Fnorte")]
    [InlineData("tour?#fragment", "/api/v1/public/catalog/tours/tour%3F%23fragment")]
    public async Task GetPublishedTourDetailsPage_escapes_the_slug_route_segment(string slug, string expectedPath)
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return PublicCatalogApiClientTestsHelpers.JsonResponse("""
                {"page":2,"pageSize":6,"total":7,"tour":{
                  "title":"First tour",
                  "slug":"first-tour",
                  "summary":"First tour summary",
                  "description":"First tour description",
                  "itinerary":"Day one: ride.",
                  "images":[],
                  "updatedAt":"2026-06-25T10:00:00+00:00"
                }}
                """);
        });
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var outcome = await sut.GetPublishedTourDetailsPage(slug, "2", TestContext.Current.CancellationToken);

        // Assert
        var page = outcome.Value.ShouldNotBeNull();
        page.Page.ShouldBe(2);
        page.Tour.Slug.ShouldBe("first-tour");
        requestPath.ShouldBe($"{expectedPath}?page=2");
    }

    [Fact]
    public async Task GetPublishedTourDetailsPage_returns_null_when_catalog_returns_notfound()
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var outcome = await sut.GetPublishedTourDetailsPage("missing-tour", page: null, TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetPublishedTourDetailsPage_respects_cancellation_before_the_response_factory_runs()
    {
        // Arrange
        var responseFactoryWasCalled = false;
        using var cancellationTokenSource = new CancellationTokenSource();
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ =>
        {
            responseFactoryWasCalled = true;
            return PublicCatalogApiClientTestsHelpers.JsonResponse("{}");
        });
        var sut = new PublicCatalogApiClient(httpClient);
        await cancellationTokenSource.CancelAsync();

        // Act
        Func<Task> act = async () => await sut.GetPublishedTourDetailsPage("cancelled", page: null, cancellationTokenSource.Token);

        // Assert
        await act.ShouldThrow<OperationCanceledException>();
        responseFactoryWasCalled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPublishedTourDetailsPage_returns_explicit_outcome_for_empty_or_null_body(bool jsonNull)
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => jsonNull
            ? PublicCatalogApiClientTestsHelpers.JsonResponse("null")
            : new HttpResponseMessage(HttpStatusCode.OK));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var outcome = await sut.GetPublishedTourDetailsPage("missing-body", page: null, TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(jsonNull
            ? HttpOutcomeKind.MalformedBody
            : HttpOutcomeKind.EmptyBody);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetPublishedTourDetailsPage_returns_unexpected_status_when_catalog_returns_unexpected_error()
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var outcome = await sut.GetPublishedTourDetailsPage("error-tour", page: null, TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.UnexpectedStatus);
        outcome.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
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
                  "language":"pt-BR",
                  "title":"Bem-vindo",
                  "body":"Pedale conosco",
                  "seoTitle":"Cicloturismo no Brasil",
                  "metaDescription":null,
                  "shareSummary":null,
                  "requiresHumanReview":false,
                  "links":[{"rel":"self","href":"/api/v1/public/catalog/content/HOME.HERO?culture=pt-BR","method":"GET"}]
                }
                """);
        });
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var outcome = await sut.GetPublicContent(key, culture, TestContext.Current.CancellationToken);

        // Assert
        var content = outcome.Value.ShouldNotBeNull();
        requestPath.ShouldBe(expectedPath);
        content.Title.ShouldBe("Bem-vindo");
        var self = content.Links.ShouldNotBeNull().ShouldHaveSingleItem();
        self.Rel.ShouldBe("self");
        self.Href.ShouldBe("/api/v1/public/catalog/content/HOME.HERO?culture=pt-BR");
        self.Method.ShouldBe("GET");
    }

    [Fact]
    public async Task GetPublicContent_returns_null_when_catalog_returns_notfound()
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var outcome = await sut.GetPublicContent("home.hero", "pt-BR", TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPublicContent_returns_explicit_outcome_for_empty_or_null_body(bool jsonNull)
    {
        // Arrange
        using var httpClient = PublicCatalogApiClientTestsHelpers.CreateClient(_ => jsonNull
            ? PublicCatalogApiClientTestsHelpers.JsonResponse("null")
            : new HttpResponseMessage(HttpStatusCode.OK));
        var sut = new PublicCatalogApiClient(httpClient);

        // Act
        var outcome = await sut.GetPublicContent("home.hero", "en-US", TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(jsonNull
            ? HttpOutcomeKind.MalformedBody
            : HttpOutcomeKind.EmptyBody);
        outcome.Value.ShouldBeNull();
    }
}
