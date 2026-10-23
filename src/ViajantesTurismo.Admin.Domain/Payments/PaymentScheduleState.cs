namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentScheduleState(Guid Id, int Number, decimal Obligation, decimal BookingPrice, PaymentInstallmentState[] Installments, string CreatedBy, DateTimeOffset CreatedAt, FinancialApprovalState? Approval, bool Superseded)
{
    public override string ToString() => "PaymentScheduleState [redacted]";
}
