using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests that a generated travel document be voided with a staff reason code.</summary>
public sealed record VoidDocumentCommand(Guid DocumentId, string Reason, DocumentAuditContext AuditContext);
