using SharedKernel.HttpClients;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Media;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class PublicGalleryEndpointTests
{
    [Fact]
    public async Task Gallery_lists_a_published_albums_cover_and_opens_its_photos_without_authentication()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(new CatalogTourDraftReadModel(tourId, Guid.CreateVersion7(), "ALBUM-1", "Camino Norte", "camino-norte", true, 1, TimeProvider.System.GetUtcNow()), ct);
        var photos = new TestPublicMediaImageStore();
        var first = PublicMediaImageTestFactory.CreateReadyImage(tourId, "first.jpg", "first-640.jpg", "sha256:first", "First", 0, false);
        var cover = PublicMediaImageTestFactory.CreateReadyImage(tourId, "cover.jpg", "cover-640.jpg", "sha256:cover", "Cover", 1, true);
        await photos.Upsert(first, ct);
        await photos.Upsert(cover, ct);
        await using var factory = CatalogApiTestHost.Create(tours, photos, new TestEventStore());
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        var catalog = new PublicCatalogApiClient(client);

        // Act
        var index = await catalog.ListPublishedAlbums("pt-BR", null, ct);
        var album = await catalog.GetPublishedAlbum("camino-norte", "pt-BR", null, ct);
        var overrun = await catalog.GetPublishedAlbum("camino-norte", "en-US", "2", ct);
        var unsupportedCulture = await catalog.ListPublishedAlbums("es-UY", null, ct);
        using var response = await client.GetAsync(new Uri("/api/v1/public/catalog/gallery", UriKind.Relative), ct);

        // Assert
        index.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        album.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        overrun.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        unsupportedCulture.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();

        var summary = index.Value.ShouldNotBeNull().Albums.ShouldHaveSingleItem();
        summary.Cover.Id.ShouldBe(cover.Id);
        summary.PhotoCount.ShouldBe(2);
        album.Value.ShouldNotBeNull().Photos.Select(photo => photo.Id).ShouldBe([first.Id, cover.Id]);
    }
}
