namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Describes the durable retirement state of an Admin tour.</summary>
public sealed record GetTourRetirementDto
{
    /// <summary>Gets the original tour identifier.</summary>
    public required Guid TourId
    {
        get; init;
    }

    /// <summary>Gets the UTC timestamp at which Admin retired the tour.</summary>
    public required DateTimeOffset RetiredAt
    {
        get; init;
    }

    /// <summary>Gets the UTC Catalog completion timestamp, or null while reconciliation is pending.</summary>
    public required DateTimeOffset? CompletedAt
    {
        get; init;
    }
}
