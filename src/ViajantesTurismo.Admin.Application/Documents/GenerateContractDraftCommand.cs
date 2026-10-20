using SharedKernel.Idempotency;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Requests generation of a customer-facing booking confirmation contract draft.
/// </summary>
public sealed record GenerateContractDraftCommand(
    Guid BookingId,
    string TemplateId,
    string TemplateVersion,
    DocumentAuditContext AuditContext,
    IdempotencyKey? IdempotencyKey = null)
{
    /// <summary>Creates a command using the current server-owned contract template.</summary>
    /// <param name="bookingId">The resource whose document is generated.</param>
    /// <param name="auditContext">Trusted metadata identifying the caller and request.</param>
    /// <param name="idempotencyKey">The caller-supplied operation key.</param>
    public GenerateContractDraftCommand(Guid bookingId, DocumentAuditContext auditContext, IdempotencyKey? idempotencyKey = null)
        : this(bookingId, ContractDocumentDraftFactory.DefaultTemplateId, ContractDocumentDraftFactory.DefaultTemplateVersion, auditContext, idempotencyKey)
    {
    }

    /// <summary>Gets the persisted operation and resource scope used for idempotency.</summary>
    public IdempotencyScope Scope => IdempotencyScope.From($"admin.documents.generate-contract-draft:{BookingId:N}");
}
