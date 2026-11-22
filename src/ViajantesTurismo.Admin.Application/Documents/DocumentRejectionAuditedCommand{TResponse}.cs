using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>A transactional document command with optional rejection evidence.</summary>
/// <typeparam name="TResponse">The typed result of the document command.</typeparam>
/// <param name="AuditContext">The authenticated actor and request correlation.</param>
public abstract record DocumentRejectionAuditedCommand<TResponse>(DocumentAuditContext AuditContext)
    : ICommand<TResponse>
    where TResponse : struct, IResultLike<TResponse>
{
    private DocumentRejectionAudit? _rejectionAudit;

    /// <summary>Gets the document operation to record in the audit trail.</summary>
    public abstract DocumentAuditOperation AuditOperation
    {
        get;
    }

    /// <summary>Clears any rejection evidence from an earlier execution attempt.</summary>
    internal void ClearRejectionAudit() => _rejectionAudit = null;

    /// <summary>Captures the bounded reason and resource identifiers available when the command was rejected.</summary>
    internal void DeferRejectionAudit(DocumentRejectionAudit rejection) => _rejectionAudit = rejection;

    /// <summary>Captures the bounded reason and resource identifiers available when the command was rejected.</summary>
    internal void DeferRejectionAudit(DocumentAuditReasonCode reasonCode, DocumentDraft? document = null) =>
        DeferRejectionAudit(new DocumentRejectionAudit(
            reasonCode,
            document?.Id,
            document?.BookingId,
            document?.Revision,
            document?.DocumentLineageId));

    /// <summary>Consumes the audit metadata captured by the last execution attempt.</summary>
    public DocumentRejectionAudit? TakeRejectionAudit()
    {
        var audit = _rejectionAudit;
        _rejectionAudit = null;
        return audit;
    }
}
