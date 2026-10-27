namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Editable form state for a booking payment schedule.</summary>
internal sealed class PaymentScheduleFormModel
{
    /// <summary>Total amount owed, in euros.</summary>
    public decimal ObligationEuro
    {
        get; set;
    }
    /// <summary>Installments the obligation is split into.</summary>
    public List<PaymentInstallmentFormRow> Installments { get; set; } = [];
    public override string ToString() => "PaymentScheduleFormModel [redacted]";
}
