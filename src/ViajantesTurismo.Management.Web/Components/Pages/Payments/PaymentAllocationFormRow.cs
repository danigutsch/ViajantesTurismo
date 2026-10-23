namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Holds one explicitly selected EUR credit target in a staff form.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515", Justification = "Used by public Blazor component parameters.")]
public sealed class PaymentAllocationFormRow
{
    /// <summary>Gets or sets the selected target.</summary>
    public Guid TargetId { get; set; } = Guid.Empty;
    /// <summary>Gets or sets the exact EUR amount entered by staff.</summary>
    public decimal EuroAmount
    {
        get; set;
    }
    /// <inheritdoc />
    public override string ToString() => "PaymentAllocationFormRow [redacted]";
}
