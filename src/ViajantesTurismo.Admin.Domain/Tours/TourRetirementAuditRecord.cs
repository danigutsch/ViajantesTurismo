using PandaBear.AuditTrail;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>Metadata-only evidence of an accepted Tour retirement.</summary>
/// <param name="RetirementId">The stable retirement identifier.</param>
/// <param name="TourId">The retired tour identifier.</param>
/// <param name="ActorId">The authenticated opaque actor identifier.</param>
/// <param name="OccurredAtUtc">The UTC retirement instant.</param>
/// <param name="Outcome">The outcome of accepting retirement.</param>
public sealed record TourRetirementAuditRecord(
    Guid RetirementId,
    Guid TourId,
    string ActorId,
    DateTime OccurredAtUtc,
    ResultStatus Outcome) : IAuditTrailEntry
{
    /// <summary>The maximum supported length of an opaque actor identifier in the retirement audit.</summary>
    public const int MaxActorIdLength = 128;
}
