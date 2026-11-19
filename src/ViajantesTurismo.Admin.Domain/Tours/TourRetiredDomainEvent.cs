using PandaBear.Domain;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>Records an Admin tour's first retirement from active use.</summary>
/// <param name="TourId">The retired tour identifier.</param>
/// <param name="RetirementId">The stable retirement identifier.</param>
/// <param name="ActorId">The authenticated opaque actor identifier for the local audit.</param>
/// <param name="RetiredAt">The UTC retirement instant.</param>
public sealed record TourRetiredDomainEvent(
    Guid TourId,
    Guid RetirementId,
    string ActorId,
    DateTimeOffset RetiredAt) : IDomainEvent;
