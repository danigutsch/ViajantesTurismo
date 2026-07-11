namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests finalization of an approved document draft.</summary>
public sealed record FinalizeDocumentCommand(Guid DocumentId);
