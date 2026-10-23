namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Separates actual cash, pending credit, refunds and reconciliation differences.</summary>
public sealed record GetBookingReceiptDto
{
    /// <summary>Gets Id.</summary>
    public required Guid Id
    {
        get; init;
    }
    /// <summary>Gets SettlementCurrency.</summary>
    public required CurrencyDto SettlementCurrency
    {
        get; init;
    }
    /// <summary>Gets Gross.</summary>
    public required decimal Gross
    {
        get; init;
    }
    /// <summary>Gets Fees.</summary>
    public required decimal Fees
    {
        get; init;
    }
    /// <summary>Gets Net.</summary>
    public required decimal Net
    {
        get; init;
    }
    /// <summary>Gets PaymentDate.</summary>
    public required DateOnly PaymentDate
    {
        get; init;
    }
    /// <summary>Gets SettledAt.</summary>
    public DateTimeOffset? SettledAt
    {
        get; init;
    }
    /// <summary>Gets Reference.</summary>
    public required string Reference
    {
        get; init;
    }
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
    /// <summary>Gets the recorded settlement method.</summary>
    public required PaymentMethodDto Method
    {
        get; init;
    }
    /// <summary>Gets Reversed.</summary>
    public required bool Reversed
    {
        get; init;
    }
    /// <summary>Gets PendingReconciliation.</summary>
    public required bool PendingReconciliation
    {
        get; init;
    }
    /// <summary>Gets ApprovedEuroCredit.</summary>
    public required decimal ApprovedEuroCredit
    {
        get; init;
    }
    /// <summary>Gets UnallocatedEuroCredit.</summary>
    public required decimal UnallocatedEuroCredit
    {
        get; init;
    }
    /// <summary>Gets UnallocatedCashCredit.</summary>
    public required decimal UnallocatedCashCredit
    {
        get; init;
    }
    /// <summary>Gets RefundableCashBasis.</summary>
    public required decimal RefundableCashBasis
    {
        get; init;
    }
    /// <summary>Gets SettlementDifference.</summary>
    public required decimal SettlementDifference
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetBookingReceiptDto [redacted]";
}
