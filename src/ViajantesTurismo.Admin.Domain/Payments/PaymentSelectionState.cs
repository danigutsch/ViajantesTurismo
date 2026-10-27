namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted EUR amount selected for a target.</summary>
/// <param name="TargetId">Identifier of the selected installment or allocation.</param>
/// <param name="EuroAmount">Selected EUR amount.</param>
internal sealed record PaymentSelectionState(Guid TargetId, decimal EuroAmount)
{
    public override string ToString() => "PaymentSelectionState [redacted]";
}
