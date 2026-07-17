namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests staff review for a generated document draft.</summary>
public sealed record BeginDocumentReviewCommand(Guid DocumentId, DocumentAuditContext? AuditContext = null);
