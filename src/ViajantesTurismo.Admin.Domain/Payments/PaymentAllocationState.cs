namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted allocation of receipt EUR credit to an installment.</summary>
/// <param name="Id">Allocation identifier.</param>
/// <param name="ReceiptId">Identifier of the funding receipt.</param>
/// <param name="InstallmentId">Identifier of the credited installment.</param>
/// <param name="EuroAmount">EUR amount allocated.</param>
/// <param name="CreatedBy">Actor who created the allocation.</param>
/// <param name="CreatedAt">UTC creation instant.</param>
internal sealed record PaymentAllocationState(Guid Id, Guid ReceiptId, Guid InstallmentId, decimal EuroAmount, string CreatedBy, DateTimeOffset CreatedAt)
{
    public override string ToString() => "PaymentAllocationState [redacted]";
}
