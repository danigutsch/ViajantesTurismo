namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests the mediated artifact for a finalized document revision.</summary>
public sealed record GetFinalizedDocumentArtifactQuery(Guid DocumentId);
