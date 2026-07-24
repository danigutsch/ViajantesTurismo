using SharedKernel.Idempotency;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests a refreshed document draft revision from current booking source data.</summary>
public sealed record RegenerateDocumentDraftCommand(
    Guid DocumentId,
    string TemplateId,
    string TemplateVersion,
    DocumentAuditContext AuditContext,
    IdempotencyKey? IdempotencyKey = null);
