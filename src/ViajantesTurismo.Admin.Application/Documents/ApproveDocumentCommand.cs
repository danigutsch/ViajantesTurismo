using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests approval of a generated document draft.</summary>
public sealed record ApproveDocumentCommand(Guid DocumentId, DocumentAuditContext AuditContext)
    : DocumentRejectionAuditedCommand(DocumentId, AuditContext)
{
    /// <inheritdoc />
    public override DocumentAuditOperation AuditOperation => DocumentAuditOperation.Approve;
}
