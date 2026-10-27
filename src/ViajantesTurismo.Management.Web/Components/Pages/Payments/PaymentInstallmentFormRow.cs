namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Editable form row for one scheduled installment.</summary>
internal sealed class PaymentInstallmentFormRow
{
    /// <summary>Installment amount in euros.</summary>
    public decimal EuroAmount
    {
        get; set;
    }
    /// <summary>Date the installment falls due.</summary>
    public DateOnly DueDate
    {
        get; set;
    }
    public override string ToString() => "PaymentInstallmentFormRow [redacted]";
}
