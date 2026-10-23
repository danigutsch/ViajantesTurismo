namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

internal sealed class PaymentQuoteFormModel
{
    public decimal BrlPerEuro
    {
        get; set;
    }
    public string Source { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime? AgreedAtUtc
    {
        get; set;
    }
    public DateTime? ExpiresAtUtc
    {
        get; set;
    }
    public bool ForRefund
    {
        get; set;
    }
    public override string ToString() => "PaymentQuoteFormModel [redacted]";
}
