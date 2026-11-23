namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains authoritative financial totals and explicitly bounded financial record and choice windows.</summary>
public sealed record GetBookingFinancialPageDto
{
    /// <summary>Gets the complete ledger summary and the requested windows of retained records.</summary>
    public required GetBookingFinancialHistoryDto History
    {
        get; init;
    }

    /// <summary>Gets the record and choice windows supplied in the response.</summary>
    public required IReadOnlyList<BookingFinancialPageWindowDto> Windows
    {
        get; init;
    }

    /// <summary>Gets identifiers of displayed records whose latest review requests changes.</summary>
    public IReadOnlyList<Guid> AttentionRequested { get; init; } = [];

    /// <summary>Gets the requested receipt choices.</summary>
    public IReadOnlyList<GetBookingReceiptDto> ReceiptChoices { get; init; } = [];

    /// <summary>Gets the requested original settlement quote choices, including the instruction-specific approved view.</summary>
    public IReadOnlyList<GetPaymentQuoteDto> SettlementQuoteChoices { get; init; } = [];

    /// <summary>Gets the requested refund quote choices.</summary>
    public IReadOnlyList<GetPaymentQuoteDto> RefundQuoteChoices { get; init; } = [];

    /// <summary>Gets installments from the approved schedule, including the instruction-specific outstanding view.</summary>
    public IReadOnlyList<GetPaymentInstallmentDto> ApprovedInstallmentChoices { get; init; } = [];

    /// <summary>Gets the requested installments from the latest schedule revision.</summary>
    public IReadOnlyList<GetPaymentInstallmentDto> ProposedInstallmentChoices { get; init; } = [];

    /// <summary>Gets the requested allocation choices that have active credit.</summary>
    public IReadOnlyList<GetPaymentAllocationDto> ActiveAllocationChoices { get; init; } = [];

    /// <inheritdoc />
    public override string ToString() => "GetBookingFinancialPageDto [redacted]";
}
