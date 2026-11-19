namespace ViajantesTurismo.Admin.Application.Tours;

/// <summary>Records Catalog completion for an existing Admin tour retirement.</summary>
public interface ITourRetirementCompletion
{
    /// <summary>Records the first matching Catalog completion and ignores stale or duplicate deliveries.</summary>
    /// <param name="tourId">The Admin tour identifier carried by the delivery.</param>
    /// <param name="retirementId">The stable retirement identifier carried by the delivery.</param>
    /// <param name="appliedAt">The timestamp when Catalog completed visibility withdrawal.</param>
    /// <param name="ct">The cancellation token for the write.</param>
    Task RecordApplied(Guid tourId, Guid retirementId, DateTimeOffset appliedAt, CancellationToken ct);
}
