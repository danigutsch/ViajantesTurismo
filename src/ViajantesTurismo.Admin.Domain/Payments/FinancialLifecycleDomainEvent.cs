using PandaBear.Domain;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Contains opaque identifiers, actors and bounded codes only, never financial payloads.</summary>
public sealed record FinancialLifecycleDomainEvent(
    Guid Id, Guid BookingId, long Version, Guid TargetId, string ActorId, DateTimeOffset OccurredAt, string Operation, FinancialReason Reason, bool SelfApproved) : IDomainEvent;
