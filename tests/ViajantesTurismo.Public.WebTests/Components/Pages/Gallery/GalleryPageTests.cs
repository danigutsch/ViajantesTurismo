using AngleSharp.Html.Parser;
using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests.Components.Pages.Gallery;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class GalleryPageTests
{
    [Fact]
    public async Task Localized_gallery_index_links_to_albums_and_only_shows_the_cover()
    {
        // Arrange
        var cover = PublicTourTestData.CreateImage("The cover") with
        {
            IsCover = true,
            SortOrder = 1
        };
        var first = PublicTourTestData.CreateImage("The first photo");
        var tour = PublicTourTestData.CreateSummary("camino-norte", "Camino Norte", includeImage: false) with
        {
            Images = [first, cover]
        };
        var catalog = new FakePublicCatalogApiClient();
        catalog.AddTour(tour);
        catalog.AddTour(PublicTourTestData.CreateSummary("andes-ride", "Andes Ride", includeImage: false));
        await using var factory = PublicWebTestHost.CreateFactory(catalog);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetStringAsync(new Uri("/pt/gallery", UriKind.Relative), TestContext.Current.CancellationToken);
        var html = WebUtility.HtmlDecode(response);

        // Assert
        html.ShouldContain("<h1 class=\"site-section__title\">Galeria</h1>", StringComparison.Ordinal);
        html.ShouldContain("href=\"/pt/gallery/camino-norte\"", StringComparison.Ordinal);
        html.ShouldContain("lang=\"en-US\"", StringComparison.Ordinal);
        html.ShouldContain("Camino Norte", StringComparison.Ordinal);
        html.ShouldContain("2 fotos", StringComparison.Ordinal);
        html.ShouldContain($"src=\"/catalog/media/{cover.Id}/640/jpg\"", StringComparison.Ordinal);
        html.ShouldNotContain(first.Id.ToString(), StringComparison.Ordinal);
        html.ShouldNotContain("Andes Ride", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Album_page_has_its_own_canonical_URL_and_preserves_responsive_photos_and_captions()
    {
        // Arrange
        var first = PublicTourTestData.CreateImage("The first photo");
        var last = PublicTourTestData.CreateImage("Ciclistas no Caminho", "Caminho em outubro") with
        {
            SortOrder = 24,
            ResponsiveVariants =
            [
                new() { Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 },
                new() { Width = 640, Height = 427, ContentType = "image/webp", FileSizeBytes = 1024 },
                new() { Width = 640, Height = 427, ContentType = "image/avif", FileSizeBytes = 1024 }
            ]
        };
        var tour = PublicTourTestData.CreateSummary("camino-norte", "Camino Norte", includeImage: false) with
        {
            Images = [first, .. Enumerable.Range(1, 23).Select(order => PublicTourTestData.CreateImage("On the Camino") with { SortOrder = order }), last]
        };
        var catalog = new FakePublicCatalogApiClient();
        catalog.AddTour(tour);
        await using var factory = PublicWebTestHost.CreateFactory(catalog);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetStringAsync(new Uri("/pt/gallery/camino-norte?page=2", UriKind.Relative), TestContext.Current.CancellationToken);
        var html = WebUtility.HtmlDecode(response);

        // Assert
        html.ShouldContain("id=\"album-camino-norte\"", StringComparison.Ordinal);
        html.ShouldContain("href=\"https://localhost:7003/pt/gallery/camino-norte?page=2\"", StringComparison.Ordinal);
        html.ShouldContain("href=\"/pt/gallery/camino-norte\"", StringComparison.Ordinal);
        html.ShouldContain($"src=\"/catalog/media/{last.Id}/640/jpg\"", StringComparison.Ordinal);
        html.ShouldContain($"/catalog/media/{last.Id}/640/webp 640w", StringComparison.Ordinal);
        html.ShouldContain($"/catalog/media/{last.Id}/640/avif 640w", StringComparison.Ordinal);
        html.ShouldContain("alt=\"Ciclistas no Caminho\"", StringComparison.Ordinal);
        html.ShouldContain(">Caminho em outubro</figcaption>", StringComparison.Ordinal);
        html.ShouldNotContain(first.Id.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/en/gallery?page=0")]
    [InlineData("/en/gallery?page=typo")]
    [InlineData("/pt/gallery/unknown")]
    [InlineData("/en/gallery/camino-norte?page=2")]
    public async Task Missing_albums_and_invalid_pages_return_not_found(string path)
    {
        // Arrange
        var catalog = new FakePublicCatalogApiClient();
        catalog.AddTour(PublicTourTestData.CreateSummary("camino-norte", "Camino Norte", includeImage: true));
        await using var factory = PublicWebTestHost.CreateFactory(catalog);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/en/gallery", "New photos will be published soon.")]
    [InlineData("/pt/gallery", "Novas fotos serão publicadas em breve.")]
    public async Task The_empty_gallery_shows_a_localized_message(string path, string message)
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), ct);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(ct));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain(message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/en/gallery", false, "The gallery could not be loaded right now.")]
    [InlineData("/pt/gallery", true, "Não foi possível carregar a galeria.")]
    [InlineData("/en/gallery/coastal-ride", false, "This album could not be loaded right now.")]
    [InlineData("/pt/gallery/coastal-ride", true, "Não foi possível carregar este álbum.")]
    public async Task An_unavailable_gallery_shows_a_localized_error_without_caching(string path, bool canceled, string message)
    {
        // Arrange
        var catalog = new FakePublicCatalogApiClient
        {
            FailListRequests = !canceled,
            FailDetailsRequests = !canceled,
            ThrowOperationCanceledExceptionOnListRequests = canceled,
            ThrowOperationCanceledExceptionOnDetailsRequests = canceled
        };
        const string tourTitle = "Coastal Ride";
        catalog.AddTour(PublicTourTestData.CreateSummary("coastal-ride", tourTitle, includeImage: true));
        await using var factory = PublicWebTestHost.CreateFactory(catalog);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), ct);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(ct));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        html.ShouldContain(message, StringComparison.Ordinal);
        html.ShouldNotContain(tourTitle, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Localized_album_identifies_the_language_of_reviewed_fallback_photo_text()
    {
        // Arrange
        var fallback = PublicTourTestData.CreateImage("Cyclists on the coast", "A ride in October") with
        {
            TextLanguage = "en-US"
        };
        var translated = PublicTourTestData.CreateImage("Ciclistas na costa", "Passeio em outubro") with
        {
            SortOrder = 1
        };
        var tour = PublicTourTestData.CreateSummary("coastal-ride", "Coastal Ride", includeImage: false) with
        {
            Images = [fallback, translated]
        };
        var catalog = new FakePublicCatalogApiClient();
        catalog.AddTour(tour);
        await using var factory = PublicWebTestHost.CreateFactory(catalog);
        using var client = factory.CreateClient();

        // Act
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetStringAsync(new Uri("/pt/gallery/coastal-ride", UriKind.Relative), ct);
        using var document = await new HtmlParser().ParseDocumentAsync(response, ct);

        // Assert
        var fallbackImage = document.QuerySelector("img[alt='Cyclists on the coast']").ShouldNotBeNull();
        fallbackImage.GetAttribute("lang").ShouldBe("en-US");
        var fallbackCaption = fallbackImage.Closest("figure").ShouldNotBeNull().QuerySelector("figcaption span").ShouldNotBeNull();
        fallbackCaption.GetAttribute("lang").ShouldBe("en-US");
        fallbackCaption.TextContent.ShouldBe("A ride in October");
        var translatedImage = document.QuerySelector("img[alt='Ciclistas na costa']").ShouldNotBeNull();
        var translatedCaption = translatedImage.Closest("figure").ShouldNotBeNull().QuerySelector("figcaption").ShouldNotBeNull();
        translatedImage.GetAttribute("lang").ShouldBeNull();
        translatedCaption.TextContent.ShouldBe("Passeio em outubro");
    }
}
