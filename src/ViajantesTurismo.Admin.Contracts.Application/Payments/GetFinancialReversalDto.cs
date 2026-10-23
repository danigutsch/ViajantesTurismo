namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Preserves append-only receipt reversals and refund-record corrections.</summary>
public sealed record GetFinancialReversalDto
{
    /// <summary>Gets reversed allocation identifiers for a full receipt reversal.</summary>
    public IReadOnlyList<Guid> AllocationIds { get; init; } = [];
    /// <summary>Gets released EUR credit for an allocation release.</summary>
    public decimal? EuroAmount
    {
        get; init;
    }
    /// <summary>Gets Id.</summary>
    public required Guid Id
    {
        get; init;
    }
    /// <summary>Gets TargetId.</summary>
    public required Guid TargetId
    {
        get; init;
    }
    /// <summary>Gets Operation.</summary>
    public required string Operation
    {
        get; init;
    }
    /// <summary>Gets ApprovedBy.</summary>
    public required string ApprovedBy
    {
        get; init;
    }
    /// <summary>Gets ApprovedAt.</summary>
    public required DateTimeOffset ApprovedAt
    {
        get; init;
    }
    /// <summary>Gets Reason.</summary>
    public required FinancialReasonDto Reason
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetFinancialReversalDto [redacted]";
}
