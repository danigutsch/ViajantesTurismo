using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Reports committed metadata for one accepted Gallery placement.</summary>
/// <param name="IntentId">The stable journal identifier for the placement.</param>
/// <param name="Status">The state observed in the committed event history.</param>
/// <param name="Revision">The terminal event's committed stream revision, when available.</param>
/// <param name="RecordedAt">The terminal event's recorded time, when available.</param>
/// <param name="AcceptedAt">When the placement was accepted, when known for an existing intent.</param>
/// <param name="TerminalEventId">The committed event that applied, rejected, or closed the placement, when present.</param>
/// <param name="CausationEventId">The persisted cause of the terminal event, when known.</param>
/// <param name="Retirement">The withdrawal matching the owning tour and committed retirement, when recorded.</param>
internal sealed record CatalogGalleryProcessStatus(
    Guid IntentId, CatalogGalleryPlacementStatusDto Status, long? Revision, DateTimeOffset? RecordedAt,
    DateTimeOffset? AcceptedAt, Guid? TerminalEventId, Guid? CausationEventId, CatalogGalleryRetirementDto? Retirement);
