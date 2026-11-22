using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Acknowledges the source or template changes of a regenerated document draft.</summary>
public sealed record AcknowledgeDocumentChangesCommand(Guid DocumentId, DocumentAuditContext AuditContext)
    : DocumentRejectionAuditedCommand(DocumentId, AuditContext)
{
    /// <inheritdoc />
    public override DocumentAuditOperation AuditOperation => DocumentAuditOperation.AcknowledgeChanges;
}
