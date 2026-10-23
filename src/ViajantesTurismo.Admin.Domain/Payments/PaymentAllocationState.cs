namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentAllocationState(Guid Id, Guid ReceiptId, Guid InstallmentId, decimal EuroAmount, string CreatedBy, DateTimeOffset CreatedAt)
{
    public override string ToString() => "PaymentAllocationState [redacted]";
}
