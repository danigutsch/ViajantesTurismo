namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentSelectionState(Guid TargetId, decimal EuroAmount)
{
    public override string ToString() => "PaymentSelectionState [redacted]";
}
