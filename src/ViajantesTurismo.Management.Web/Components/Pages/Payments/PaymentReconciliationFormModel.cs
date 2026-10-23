namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

internal sealed class PaymentReconciliationFormModel
{
    public Guid ReceiptId { get; set; } = Guid.Empty;
    public decimal EuroCredit
    {
        get; set;
    }
    public Guid? QuoteId
    {
        get; set;
    }
    public Guid? InstructionId
    {
        get; set;
    }
    public List<PaymentAllocationFormRow> Allocations { get; set; } = [];
    public override string ToString() => "PaymentReconciliationFormModel [redacted]";
}
