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
}
