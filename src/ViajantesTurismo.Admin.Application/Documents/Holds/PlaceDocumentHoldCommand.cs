using PandaBear.DispositionHold;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Requests a disposition hold on the lineage of a document.</summary>
/// <param name="DocumentId">Any revision of the lineage to hold.</param>
/// <param name="Basis">Why the hold exists.</param>
/// <param name="PurposeCode">The opaque purpose code.</param>
/// <param name="AuthorityReference">The opaque reference to the authorizing record.</param>
/// <param name="ReviewDueAt">When the hold is next due for review.</param>
/// <param name="ExpiresAt">When the hold stops restraining by itself, or <see langword="null" />.</param>
/// <param name="ActorId">The opaque id of the authenticated principal; never taken from a request body.</param>
public sealed record PlaceDocumentHoldCommand(
    Guid DocumentId,
    HoldBasis Basis,
    string PurposeCode,
    string AuthorityReference,
    DateTimeOffset ReviewDueAt,
    DateTimeOffset? ExpiresAt,
    string ActorId);
