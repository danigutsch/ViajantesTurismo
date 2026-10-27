using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Editable form state for recording a payment receipt.</summary>
internal sealed class PaymentReceiptFormModel
{
    /// <summary>Currency in which the payment settled.</summary>
    public CurrencyDto SettlementCurrency { get; set; } = CurrencyDto.Euro;
    /// <summary>Gross amount received, in the settlement currency.</summary>
    public decimal Gross
    {
        get; set;
    }
    /// <summary>Fees deducted from the gross amount, in the settlement currency.</summary>
    public decimal Fees
    {
        get; set;
    }
    /// <summary>Calendar date of the payment.</summary>
    public DateOnly PaymentDate
    {
        get; set;
    }
    /// <summary>UTC moment the payment settled, or null when not set.</summary>
    public DateTime? SettledAtUtc
    {
        get; set;
    }
    /// <summary>How the payment was made.</summary>
    public PaymentMethodDto Method { get; set; } = PaymentMethodDto.BankTransfer;
    /// <summary>External reference identifying the payment.</summary>
    public string Reference { get; set; } = string.Empty;
    /// <summary>Rows allocating the receipt across obligations.</summary>
    public List<PaymentAllocationFormRow> Allocations { get; set; } = [];
    public override string ToString() => "PaymentReceiptFormModel [redacted]";
}
