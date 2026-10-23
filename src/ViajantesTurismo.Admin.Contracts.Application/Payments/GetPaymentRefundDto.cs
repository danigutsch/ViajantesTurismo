namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Separates original receipt cash basis, newly quoted actual settlement and FX differences.</summary>
public sealed record GetPaymentRefundDto
{
    /// <summary>Gets Id.</summary>
    public required Guid Id
    {
        get; init;
    }
    /// <summary>Gets ReceiptId.</summary>
    public required Guid ReceiptId
    {
        get; init;
    }
    /// <summary>Gets OriginalCashBasis.</summary>
    public required decimal OriginalCashBasis
    {
        get; init;
    }
    /// <summary>Gets EuroCreditReversed.</summary>
    public required decimal EuroCreditReversed
    {
        get; init;
    }
    /// <summary>Gets EuroCashPayable.</summary>
    public required decimal EuroCashPayable
    {
        get; init;
    }
    /// <summary>Gets ActualSettlement.</summary>
    public required decimal ActualSettlement
    {
        get; init;
    }
    /// <summary>Gets FxDifference.</summary>
    public required decimal FxDifference
    {
        get; init;
    }
    /// <summary>Gets QuoteId.</summary>
    public Guid? QuoteId
    {
        get; init;
    }
    /// <summary>Gets Reference.</summary>
    public required string Reference
    {
        get; init;
    }
    /// <summary>Gets PerformedAt.</summary>
    public required DateTimeOffset PerformedAt
    {
        get; init;
    }
    /// <summary>Gets Corrected.</summary>
    public required bool Corrected
    {
        get; init;
    }
    /// <summary>Gets ReversedAllocations.</summary>
    public required IReadOnlyList<InstallmentAllocationDto> ReversedAllocations
    {
        get; init;
    }
    /// <summary>Gets Approval.</summary>
    public required FinancialApprovalDto Approval
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetPaymentRefundDto [redacted]";
}
