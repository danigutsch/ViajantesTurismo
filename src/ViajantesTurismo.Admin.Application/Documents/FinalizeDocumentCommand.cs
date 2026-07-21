using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests finalization of an approved document draft.</summary>
public sealed record FinalizeDocumentCommand(Guid DocumentId, DocumentAuditContext? AuditContext = null);
