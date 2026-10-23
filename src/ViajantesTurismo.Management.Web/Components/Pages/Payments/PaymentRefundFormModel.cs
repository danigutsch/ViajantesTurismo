namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

internal sealed class PaymentRefundFormModel
{
    public Guid ReceiptId { get; set; } = Guid.Empty;
    public decimal ActualSettlement
    {
        get; set;
    }
    public decimal OriginalCashBasis
    {
        get; set;
    }
    public decimal EuroCreditReversed
    {
        get; set;
    }
    public decimal EuroCashPayable
    {
        get; set;
    }
    public Guid? RefundQuoteId
    {
        get; set;
    }
    public string Reference { get; set; } = string.Empty;
    public DateTime? PerformedAtUtc
    {
        get; set;
    }
    public bool PerformedAttested
    {
        get; set;
    }
    public List<PaymentAllocationFormRow> Allocations { get; set; } = [];
    public override string ToString() => "PaymentRefundFormModel [redacted]";
}
