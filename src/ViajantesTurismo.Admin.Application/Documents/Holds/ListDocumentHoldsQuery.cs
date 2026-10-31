namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Requests the holds of the lineage that contains a document.</summary>
/// <param name="DocumentId">Any revision of the lineage.</param>
public sealed record ListDocumentHoldsQuery(Guid DocumentId);
