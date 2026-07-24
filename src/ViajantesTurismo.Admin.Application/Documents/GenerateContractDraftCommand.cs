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
    IdempotencyKey? IdempotencyKey = null);
