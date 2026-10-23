namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains immutable EUR terms and masked settlement history, with overdue separate from payment state.</summary>
public sealed record GetBookingFinancialHistoryDto
{
    /// <summary>Gets BookingId.</summary>
    public required Guid BookingId
    {
        get; init;
    }
    /// <summary>Gets Version.</summary>
    public required long Version
    {
        get; init;
    }
    /// <summary>Gets ObligationCurrency.</summary>
    public required CurrencyDto ObligationCurrency
    {
        get; init;
    }
    /// <summary>Gets TermsStale.</summary>
    public required bool TermsStale
    {
        get; init;
    }
    /// <summary>Gets ApprovedObligationEuro.</summary>
    public required decimal ApprovedObligationEuro
    {
        get; init;
    }
    /// <summary>Gets OutstandingEuro.</summary>
    public required decimal OutstandingEuro
    {
        get; init;
    }
    /// <summary>Gets UnallocatedEuroCredit.</summary>
    public required decimal UnallocatedEuroCredit
    {
        get; init;
    }
    /// <summary>Gets BusinessDate.</summary>
    public required DateOnly BusinessDate
    {
        get; init;
    }
    /// <summary>Gets Overdue.</summary>
    public required bool Overdue
    {
        get; init;
    }
    /// <summary>Gets Schedules.</summary>
    public required IReadOnlyList<GetPaymentScheduleDto> Schedules
    {
        get; init;
    }
    /// <summary>Gets Quotes.</summary>
    public required IReadOnlyList<GetPaymentQuoteDto> Quotes
    {
        get; init;
    }
    /// <summary>Gets Receipts.</summary>
    public required IReadOnlyList<GetBookingReceiptDto> Receipts
    {
        get; init;
    }
    /// <summary>Gets Reconciliations.</summary>
    public required IReadOnlyList<GetReceiptReconciliationDto> Reconciliations
    {
        get; init;
    }
    /// <summary>Gets Allocations.</summary>
    public required IReadOnlyList<GetPaymentAllocationDto> Allocations
    {
        get; init;
    }
    /// <summary>Gets Refunds.</summary>
    public required IReadOnlyList<GetPaymentRefundDto> Refunds
    {
        get; init;
    }
    /// <summary>Gets Reversals.</summary>
    public required IReadOnlyList<GetFinancialReversalDto> Reversals
    {
        get; init;
    }
    /// <summary>Gets Reviews.</summary>
    public required IReadOnlyList<GetFinancialReviewDto> Reviews
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetBookingFinancialHistoryDto [redacted]";
}
