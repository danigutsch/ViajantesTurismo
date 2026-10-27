namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Editable form state for reconciling a receipt against obligations.</summary>
internal sealed class PaymentReconciliationFormModel
{
    /// <summary>Identifier of the receipt being reconciled.</summary>
    public Guid ReceiptId { get; set; } = Guid.Empty;
    /// <summary>Euro credit the receipt provides.</summary>
    public decimal EuroCredit
    {
        get; set;
    }
    /// <summary>Identifier of the exchange-rate quote applied, or null when none.</summary>
    public Guid? QuoteId
    {
        get; set;
    }
    /// <summary>Identifier of the payment instruction being matched, or null when none.</summary>
    public Guid? InstructionId
    {
        get; set;
    }
    /// <summary>Rows allocating the euro credit across obligations.</summary>
    public List<PaymentAllocationFormRow> Allocations { get; set; } = [];
    public override string ToString() => "PaymentReconciliationFormModel [redacted]";
}
