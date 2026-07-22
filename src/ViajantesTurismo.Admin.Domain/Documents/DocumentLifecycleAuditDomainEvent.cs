using SharedKernel.Domain;

namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Represents metadata-only evidence of one successful document lifecycle operation.</summary>
public sealed record DocumentLifecycleAuditDomainEvent(
    string ActorId,
    string CorrelationId,
    Guid DocumentId,
    Guid BookingId,
    int DocumentRevision,
    DocumentAuditOperation Operation) : IDomainEvent;
