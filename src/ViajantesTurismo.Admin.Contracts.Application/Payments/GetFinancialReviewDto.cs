namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Binds an independent reviewer and outcome to one immutable financial revision.</summary>
public sealed record GetFinancialReviewDto
{
    /// <summary>Gets TargetId.</summary>
    public required Guid TargetId
    {
        get; init;
    }
    /// <summary>Gets TargetRevision.</summary>
    public required int TargetRevision
    {
        get; init;
    }
    /// <summary>Gets ReviewerId.</summary>
    public required string ReviewerId
    {
        get; init;
    }
    /// <summary>Gets ReviewedAt.</summary>
    public required DateTimeOffset ReviewedAt
    {
        get; init;
    }
    /// <summary>Gets ChangesRequested.</summary>
    public required bool ChangesRequested
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetFinancialReviewDto [redacted]";
}
