using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

internal sealed class PaymentReceiptFormModel
{
    public CurrencyDto SettlementCurrency { get; set; } = CurrencyDto.Euro;
    public decimal Gross
    {
        get; set;
    }
    public decimal Fees
    {
        get; set;
    }
    public DateOnly PaymentDate
    {
        get; set;
    }
    public DateTime? SettledAtUtc
    {
        get; set;
    }
    public PaymentMethodDto Method { get; set; } = PaymentMethodDto.BankTransfer;
    public string Reference { get; set; } = string.Empty;
    public List<PaymentAllocationFormRow> Allocations { get; set; } = [];
    public override string ToString() => "PaymentReceiptFormModel [redacted]";
}
