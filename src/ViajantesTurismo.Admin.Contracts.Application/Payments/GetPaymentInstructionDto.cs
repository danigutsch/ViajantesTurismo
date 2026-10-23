namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Contains masked payment instruction metadata and generated financial fields; recipient details stay protected.</summary>
public sealed record GetPaymentInstructionDto
{
    /// <summary>Gets the existing document lineage projection.</summary>
    public required GetDocumentDto Document
    {
        get; init;
    }
    /// <summary>Gets the current lineage concurrency version.</summary>
    public required long DocumentVersion
    {
        get; init;
    }
    /// <summary>Gets the verified recipient account identifier.</summary>
    public required Guid AccountId
    {
        get; init;
    }
    /// <summary>Gets the pinned verified account revision.</summary>
    public required Guid AccountRevisionId
    {
        get; init;
    }
    /// <summary>Gets the masked recipient identifier.</summary>
    public required string MaskedRecipient
    {
        get; init;
    }
    /// <summary>Gets whether finalization or download is currently eligible.</summary>
    public required bool Eligible
    {
        get; init;
    }
    /// <summary>Gets the selected EUR amount.</summary>
    public required decimal EuroAmount
    {
        get; init;
    }
    /// <summary>Gets the generated settlement amount.</summary>
    public required decimal SettlementAmount
    {
        get; init;
    }
    /// <summary>Gets the explicit settlement currency.</summary>
    public required CurrencyDto SettlementCurrency
    {
        get; init;
    }
    /// <summary>Gets the frozen payment reference.</summary>
    public required string PaymentReference
    {
        get; init;
    }
    /// <summary>Gets the frozen quote expiry instant, if applicable.</summary>
    public DateTimeOffset? QuoteExpiresAt
    {
        get; init;
    }
    /// <summary>Gets creator and explicit approver metadata.</summary>
    public required FinancialApprovalDto Approval
    {
        get; init;
    }
    /// <summary>Gets revision-bound independent reviews.</summary>
    public required IReadOnlyList<GetFinancialReviewDto> Reviews
    {
        get; init;
    }
    /// <inheritdoc />
    public override string ToString() => "GetPaymentInstructionDto [redacted]";
}
