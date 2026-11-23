using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Supplies seeded photo options to the management API test host.</summary>
internal sealed class TestCatalogGalleryPhotoOptionsQuery(TestCatalogTourReadModelStore tours, TestPublicMediaImageStore media)
    : ICatalogGalleryPhotoOptionsQuery
{
    /// <inheritdoc />
    public async Task<CatalogGalleryPhotoOptionsDto?> Search(
        Guid catalogTourId, CatalogGalleryPhotoOptionsRequest request, CancellationToken ct)
    {
        if (await tours.GetTour(catalogTourId, ct) is null)
        {
            return null;
        }

        var photos = await media.ListByTour(catalogTourId, ct);
        var matches = photos.Where(photo => request.IncludedPhotoIds?.Contains(photo.Id) != false).ToArray();
        var page = matches.Take(request.Limit)
            .Select(photo => new CatalogGalleryPhotoOptionDto(photo.Id, photo.AltText, null, null)).ToArray();
        return new CatalogGalleryPhotoOptionsDto(page, matches.Length);
    }
}
