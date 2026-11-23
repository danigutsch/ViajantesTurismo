namespace ViajantesTurismo.Catalog.Application.Gallery;

/// <summary>A committed request to update one tour's authored placement.</summary>
/// <param name="Id">The stable intent identity.</param>
/// <param name="Sequence">Its position in the tour's intent journal.</param>
/// <param name="PhotoIds">The authored photo order, or the single photo to append.</param>
/// <param name="CoverId">The optional chosen cover.</param>
/// <param name="CanPublish">The publication authority held when the request was accepted.</param>
/// <param name="AppendsUploadedPhoto">Whether the intent appends an upload to an authored baseline.</param>
public sealed record CatalogGalleryPlacementIntent(
    Guid Id, long Sequence, IReadOnlyList<Guid> PhotoIds, Guid? CoverId, bool CanPublish, bool AppendsUploadedPhoto);
