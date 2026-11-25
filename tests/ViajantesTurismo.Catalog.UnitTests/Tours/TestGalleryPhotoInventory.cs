using SharedKernel.BuildingBlocks;
using SharedKernel.Galleries;

namespace ViajantesTurismo.Catalog.UnitTests.Tours;

/// <summary>Supplies the identities of placed photos, including media that still needs processing.</summary>
internal sealed class TestGalleryPhotoInventory(IEnumerable<Guid> photoIds) : IAlbumPhotoEligibility<Guid>
{
    private readonly HashSet<Guid> knownPhotos = [.. photoIds];

    /// <inheritdoc />
    public bool Exists(Guid photoId) => knownPhotos.Contains(photoId);

    /// <inheritdoc />
    public bool IsEligible(Guid photoId, ContentCulture culture) => false;
}
