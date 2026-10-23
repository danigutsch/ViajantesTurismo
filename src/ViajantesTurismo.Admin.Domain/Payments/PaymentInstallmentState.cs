namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentInstallmentState(Guid Id, decimal Amount, DateOnly DueDate)
{
    public override string ToString() => "PaymentInstallmentState [redacted]";
}
