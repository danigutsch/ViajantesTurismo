using SharedKernel.AuditTrail;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Mappings;

/// <summary>Maps an accepted retirement to its metadata-only audit entry.</summary>
internal static class TourRetirementAuditTrailMappings
{
    /// <summary>Captures the actor and transition timestamp for an accepted retirement.</summary>
    [AuditTrailMapping]
    public static TourRetirementAuditRecord Map(TourRetiredDomainEvent domainEvent, DateTimeOffset _)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return new TourRetirementAuditRecord(
            domainEvent.RetirementId,
            domainEvent.TourId,
            domainEvent.ActorId,
            domainEvent.RetiredAt.UtcDateTime,
            ResultStatus.Ok);
    }
}
