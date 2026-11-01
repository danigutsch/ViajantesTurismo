using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Requests the release of a disposition hold on a document lineage.</summary>
/// <param name="DocumentId">Any revision of the lineage the hold belongs to.</param>
/// <param name="HoldId">The hold to release.</param>
/// <param name="ReasonCode">The opaque reason code recorded with the release.</param>
/// <param name="AuditContext">The trusted opaque actor and correlation ids; never taken from a request body.</param>
public sealed record ReleaseDocumentHoldCommand(Guid DocumentId, Guid HoldId, string ReasonCode, DocumentAuditContext AuditContext);
