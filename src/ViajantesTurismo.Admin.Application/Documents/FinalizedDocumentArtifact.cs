namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Contains a finalized document artifact for a mediated delivery boundary.</summary>
public sealed record FinalizedDocumentArtifact(
    Guid DocumentId,
    Guid BookingId,
    int Revision,
    ReadOnlyMemory<byte> Content,
    string FileName);
