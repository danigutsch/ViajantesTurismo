namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests changes to a generated document draft.</summary>
public sealed record RequestDocumentChangesCommand(Guid DocumentId, DocumentAuditContext? AuditContext = null);
