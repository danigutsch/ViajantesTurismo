using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>A transactional document decision with optional rejection evidence.</summary>
/// <param name="DocumentId">The requested document identifier.</param>
/// <param name="AuditContext">The authenticated actor and request correlation.</param>
public abstract record DocumentRejectionAuditedCommand(Guid DocumentId, DocumentAuditContext AuditContext)
    : DocumentRejectionAuditedCommand<Result>(AuditContext);
