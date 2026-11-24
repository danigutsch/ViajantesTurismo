using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.UnitTests.Gallery;

public sealed class CatalogGalleryProjectionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(CatalogGalleryProjection.MaxPhotos + 1)]
    public void An_empty_or_oversized_album_is_unavailable(int count)
    {
        // Arrange
        var ids = Enumerable.Range(0, count).Select(_ => Guid.CreateVersion7()).ToArray();

        // Act
        var album = CatalogGalleryProjection.PublishedAlbum(Guid.CreateVersion7(), "Camino Norte", "camino-norte",
            ids, null, PublicContentCultures.EnglishUnitedStates);

        // Assert
        album.ShouldBeNull();
    }

    [Fact]
    public void A_maximum_size_album_keeps_existing_photo_order_and_uses_the_first_as_cover()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var ids = Enumerable.Range(0, CatalogGalleryProjection.MaxPhotos).Select(_ => Guid.CreateVersion7()).ToArray();

        // Act
        var album = CatalogGalleryProjection.PublishedAlbum(tourId, "Camino Norte", "camino-norte",
            ids, null, PublicContentCultures.PortugueseBrazil);

        // Assert
        album.ShouldNotBeNull();
        album.RelatedId.ShouldBe(tourId);
        album.Culture.ShouldBe(PublicContentCultures.EnglishUnitedStates);
        album.CoverId.ShouldBe(ids[0]);
        album.PhotoIds.ShouldBe(ids);
    }

    [Fact]
    public void A_chosen_cover_that_is_unavailable_in_this_culture_falls_back_to_the_first_eligible_photo()
    {
        // Arrange
        var first = Guid.CreateVersion7();
        var next = Guid.CreateVersion7();
        var unavailableCover = Guid.CreateVersion7();

        // Act
        var album = CatalogGalleryProjection.PublishedAlbum(Guid.CreateVersion7(), "Camino Norte", "camino-norte",
            [first, next], unavailableCover, PublicContentCultures.PortugueseBrazil);

        // Assert
        var published = album.ShouldNotBeNull();
        published.CoverId.ShouldBe(first);
        published.PhotoIds.ShouldBe([first, next]);
    }
}
