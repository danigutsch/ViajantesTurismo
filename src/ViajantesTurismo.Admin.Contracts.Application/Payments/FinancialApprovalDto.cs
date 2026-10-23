namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains creator and approver metadata with a bounded reason code.</summary>
public sealed record FinancialApprovalDto
{
    /// <summary>Gets CreatedBy.</summary>
    public required string CreatedBy
    {
        get; init;
    }
    /// <summary>Gets CreatedAt.</summary>
    public required DateTimeOffset CreatedAt
    {
        get; init;
    }
    /// <summary>Gets ApprovedBy.</summary>
    public string? ApprovedBy
    {
        get; init;
    }
    /// <summary>Gets ApprovedAt.</summary>
    public DateTimeOffset? ApprovedAt
    {
        get; init;
    }
    /// <summary>Gets ApprovalReason.</summary>
    public FinancialReasonDto? ApprovalReason
    {
        get; init;
    }
    /// <summary>Gets SelfApproved.</summary>
    public required bool SelfApproved
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "FinancialApprovalDto [redacted]";
}
