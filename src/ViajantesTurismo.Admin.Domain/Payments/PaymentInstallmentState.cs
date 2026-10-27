namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted EUR installment of a payment schedule.</summary>
/// <param name="Id">Installment identifier, used as allocation target.</param>
/// <param name="Amount">EUR amount due.</param>
/// <param name="DueDate">Civil calendar due date.</param>
internal sealed record PaymentInstallmentState(Guid Id, decimal Amount, DateOnly DueDate)
{
    public override string ToString() => "PaymentInstallmentState [redacted]";
}
