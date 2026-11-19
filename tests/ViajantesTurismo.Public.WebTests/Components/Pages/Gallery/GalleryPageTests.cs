using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests.Components.Pages.Gallery;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class GalleryPageTests
{
    [Fact]
    public async Task Localized_gallery_shows_published_photos_with_their_alternative_text_and_caption()
    {
        // Arrange
        var tour = PublicTourTestData.CreateSummary("camino-norte", "Camino Norte", includeImage: true);
        var image = tour.Images.ShouldHaveSingleItem();
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
        html.ShouldContain("Camino Norte", StringComparison.Ordinal);
        html.ShouldContain($"src=\"/catalog/media/{image.Id}/640/jpg\"", StringComparison.Ordinal);
        html.ShouldContain("alt=\"Cyclists on the Camino\"", StringComparison.Ordinal);
        html.ShouldContain("<figcaption", StringComparison.Ordinal);
        html.ShouldContain(">Camino caption</figcaption>", StringComparison.Ordinal);
        html.ShouldNotContain("Andes Ride", StringComparison.Ordinal);
    }
}
