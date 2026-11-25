using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Media;

/// <summary>Exercises the public Catalog routes with an existing authored placement projection.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class PublicAuthoredGalleryEndpointTests
{
    [Fact]
    public async Task Public_tour_summary_and_details_both_observe_authored_order_and_cover()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await PublicGalleryPlacementScenario.Create(secondIncludesShared: false, ct);
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();

        // Act
        var list = await client.GetFromJsonAsync<PublishedTourPageDto>(new Uri("/api/v1/public/catalog/tours/browse/page", UriKind.Relative), ct);
        var detail = await client.GetFromJsonAsync<PublishedTourDetailsPageDto>(new Uri("/api/v1/public/catalog/tours/first", UriKind.Relative), ct);

        // Assert
        var summary = list.ShouldNotBeNull().Tours.Where(tour => tour.Slug == "first").ShouldHaveSingleItem();
        summary.Images.Select(photo => photo.Id).ShouldBe([scenario.CoverId]);
        summary.Images.Where(photo => photo.IsCover).ShouldHaveSingleItem().Id.ShouldBe(scenario.CoverId);
        var tour = detail.ShouldNotBeNull().Tour;
        tour.Images.Select(photo => photo.Id).ShouldBe([scenario.CoverId, scenario.FirstId]);
        tour.Images.Where(photo => photo.IsCover).ShouldHaveSingleItem().Id.ShouldBe(scenario.CoverId);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.NotFound)]
    [InlineData(true, HttpStatusCode.OK)]
    public async Task Public_rendition_requires_one_visible_gallery_to_include_the_linked_photo(bool secondIncludesShared, HttpStatusCode status)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await PublicGalleryPlacementScenario.Create(secondIncludesShared, ct);
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();
        var uri = new Uri($"/api/v1/public/catalog/media/{scenario.OmittedId}/640/jpg", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);

        // Assert
        response.StatusCode.ShouldBe(status);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task Public_gallery_excludes_an_explicitly_empty_album_with_remaining_inventory_links()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await PublicGalleryPlacementScenario.Create(secondIncludesShared: false, ct);
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();

        // Act
        var index = await client.GetFromJsonAsync<CatalogGalleryIndexDto>(new Uri("/api/v1/public/catalog/gallery", UriKind.Relative), ct);
        using var empty = await client.GetAsync(new Uri("/api/v1/public/catalog/gallery/second", UriKind.Relative), ct);

        // Assert
        var album = index.ShouldNotBeNull().Albums.ShouldHaveSingleItem();
        album.Slug.ShouldBe("first");
        album.Cover.Id.ShouldBe(scenario.CoverId);
        empty.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
