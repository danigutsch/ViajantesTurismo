namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

internal sealed class PaymentInstallmentFormRow
{
    public decimal EuroAmount
    {
        get; set;
    }
    public DateOnly DueDate
    {
        get; set;
    }
    public override string ToString() => "PaymentInstallmentFormRow [redacted]";
}
