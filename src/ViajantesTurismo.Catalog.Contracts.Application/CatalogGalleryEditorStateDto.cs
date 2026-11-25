namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Describes the current authored snapshot and revision tokens for one Catalog tour.</summary>
/// <param name="StreamVersion">The authoritative tour event-stream revision.</param>
/// <param name="ProjectedStreamVersion">The revision represented by the management projection.</param>
/// <param name="AcceptedPlacementSequence">The highest accepted placement sequence, or zero for an empty journal.</param>
/// <param name="ResolvedPlacementSequence">The highest placement sequence resolved in the event stream.</param>
/// <param name="PhotoIds">The authored photo order, or null while placement still uses legacy links.</param>
/// <param name="CoverId">The explicit authored cover, when present.</param>
/// <param name="IsRetired">Whether the tour is permanently closed.</param>
/// <param name="CanEditGallery">Whether the caller's current permissions permit gallery changes.</param>
/// <param name="MaximumPhotoCount">The Catalog album's photo limit.</param>
/// <param name="Outcome">The requested or latest placement outcome, when a placement exists.</param>
public sealed record CatalogGalleryEditorStateDto(
    long StreamVersion,
    long ProjectedStreamVersion,
    long AcceptedPlacementSequence,
    long ResolvedPlacementSequence,
    IReadOnlyList<Guid>? PhotoIds,
    Guid? CoverId,
    bool IsRetired,
    bool CanEditGallery,
    int MaximumPhotoCount,
    CatalogGalleryPlacementOutcomeDto? Outcome);
