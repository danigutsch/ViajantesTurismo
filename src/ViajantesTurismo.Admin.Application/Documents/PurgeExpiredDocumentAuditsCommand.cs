using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Requests removal of document audit records whose retention period has elapsed.</summary>
/// <param name="AuditContext">The opaque system identity and correlation id recorded as purge evidence.</param>
public sealed record PurgeExpiredDocumentAuditsCommand(DocumentAuditContext AuditContext);
