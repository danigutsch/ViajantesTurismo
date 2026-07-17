namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests approval of a generated document draft.</summary>
public sealed record ApproveDocumentCommand(Guid DocumentId, DocumentAuditContext? AuditContext = null);
