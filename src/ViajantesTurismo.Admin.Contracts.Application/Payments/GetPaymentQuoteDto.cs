namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains an agreed quote and its approval metadata; ordinary projections mask evidence.</summary>
public sealed record GetPaymentQuoteDto
{
    /// <summary>Gets Id.</summary>
    public required Guid Id
    {
        get; init;
    }
    /// <summary>Gets BrlPerEuro.</summary>
    public required decimal BrlPerEuro
    {
        get; init;
    }
    /// <summary>Gets Source.</summary>
    public required string Source
    {
        get; init;
    }
    /// <summary>Gets Reference.</summary>
    public required string Reference
    {
        get; init;
    }
    /// <summary>Gets AgreedAt.</summary>
    public required DateTimeOffset AgreedAt
    {
        get; init;
    }
    /// <summary>Gets ExpiresAt.</summary>
    public required DateTimeOffset ExpiresAt
    {
        get; init;
    }
    /// <summary>Gets ForRefund.</summary>
    public required bool ForRefund
    {
        get; init;
    }
    /// <summary>Gets ApprovalWithdrawn.</summary>
    public required bool ApprovalWithdrawn
    {
        get; init;
    }
    /// <summary>Gets Approval.</summary>
    public required FinancialApprovalDto Approval
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetPaymentQuoteDto [redacted]";
}
