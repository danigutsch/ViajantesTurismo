namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Editable form state for recording a refund of a receipt.</summary>
internal sealed class PaymentRefundFormModel
{
    /// <summary>Identifier of the receipt being refunded.</summary>
    public Guid ReceiptId { get; set; } = Guid.Empty;
    /// <summary>Amount actually settled to the payer, in the settlement currency.</summary>
    public decimal ActualSettlement
    {
        get; set;
    }
    /// <summary>Original cash amount the refund is measured against.</summary>
    public decimal OriginalCashBasis
    {
        get; set;
    }
    /// <summary>Euro credit reversed by the refund.</summary>
    public decimal EuroCreditReversed
    {
        get; set;
    }
    /// <summary>Euro cash amount payable back to the payer.</summary>
    public decimal EuroCashPayable
    {
        get; set;
    }
    /// <summary>Identifier of the refund exchange-rate quote, or null when none.</summary>
    public Guid? RefundQuoteId
    {
        get; set;
    }
    /// <summary>External reference identifying the refund.</summary>
    public string Reference { get; set; } = string.Empty;
    /// <summary>UTC moment the refund was performed, or null when not set.</summary>
    public DateTime? PerformedAtUtc
    {
        get; set;
    }
    /// <summary>Whether the operator attests the refund was actually performed.</summary>
    public bool PerformedAttested
    {
        get; set;
    }
    /// <summary>Rows allocating the refund across obligations.</summary>
    public List<PaymentAllocationFormRow> Allocations { get; set; } = [];
    public override string ToString() => "PaymentRefundFormModel [redacted]";
}
