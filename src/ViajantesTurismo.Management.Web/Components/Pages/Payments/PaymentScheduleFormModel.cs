namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

internal sealed class PaymentScheduleFormModel
{
    public decimal ObligationEuro
    {
        get; set;
    }
    public List<PaymentInstallmentFormRow> Installments { get; set; } = [];
    public override string ToString() => "PaymentScheduleFormModel [redacted]";
}
