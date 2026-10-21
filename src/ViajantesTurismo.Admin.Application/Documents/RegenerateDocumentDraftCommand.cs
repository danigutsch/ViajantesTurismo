using PandaBear.Idempotency;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests a refreshed document draft revision from current booking source data.</summary>
public sealed record RegenerateDocumentDraftCommand(
    Guid DocumentId,
    string TemplateId,
    string TemplateVersion,
    DocumentAuditContext AuditContext,
    IdempotencyKey IdempotencyKey)
{
    /// <summary>Creates a command using the current server-owned contract template.</summary>
    /// <param name="documentId">The resource whose document is generated.</param>
    /// <param name="auditContext">Trusted metadata identifying the caller and request.</param>
    /// <param name="idempotencyKey">The required caller-supplied operation key.</param>
    public RegenerateDocumentDraftCommand(Guid documentId, DocumentAuditContext auditContext, IdempotencyKey idempotencyKey)
        : this(documentId, ContractDocumentDraftFactory.DefaultTemplateId, ContractDocumentDraftFactory.DefaultTemplateVersion, auditContext, idempotencyKey)
    {
    }

    /// <summary>Gets the persisted operation and resource scope used for idempotency.</summary>
    public IdempotencyScope Scope => IdempotencyScope.From($"admin.documents.regenerate-draft:{DocumentId:N}");
}
