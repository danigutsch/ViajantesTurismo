namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Requests one hold of the lineage that contains a document.</summary>
/// <param name="DocumentId">Any revision of the lineage.</param>
/// <param name="HoldId">The hold identifier.</param>
public sealed record GetDocumentHoldQuery(Guid DocumentId, Guid HoldId);
