using PandaBear.BuildingBlocks;
using PandaBear.Galleries;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Application.Gallery;

/// <summary>Supplies the Catalog media linked to the tour being edited.</summary>
internal sealed class CatalogGalleryPhotoInventory(IReadOnlyList<PublicMediaImage> images) : IAlbumPhotoEligibility<Guid>
{
    private readonly Dictionary<Guid, PublicMediaImage> imagesById = images.ToDictionary(image => image.Id);

    /// <inheritdoc />
    public bool Exists(Guid photoId) => imagesById.ContainsKey(photoId);

    /// <inheritdoc />
    public bool IsEligible(Guid photoId, ContentCulture culture) =>
        imagesById.TryGetValue(photoId, out var image) && image.GetGalleryAccessibility(culture) is not null;
}
