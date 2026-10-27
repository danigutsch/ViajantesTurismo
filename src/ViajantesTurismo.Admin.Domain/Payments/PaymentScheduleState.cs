namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted payment schedule revision.</summary>
/// <param name="Id">Schedule revision identifier.</param>
/// <param name="Number">Sequential revision number starting at 1.</param>
/// <param name="Obligation">Total EUR obligation.</param>
/// <param name="BookingPrice">Booking price in EUR the schedule was drafted against.</param>
/// <param name="Installments">Installments summing to the obligation.</param>
/// <param name="CreatedBy">Actor who drafted the schedule.</param>
/// <param name="CreatedAt">UTC draft instant.</param>
/// <param name="Approval">Approval of the schedule; null while it is a draft.</param>
/// <param name="Superseded">Whether a later approved revision replaced this one.</param>
internal sealed record PaymentScheduleState(Guid Id, int Number, decimal Obligation, decimal BookingPrice, PaymentInstallmentState[] Installments, string CreatedBy, DateTimeOffset CreatedAt, FinancialApprovalState? Approval, bool Superseded)
{
    public override string ToString() => "PaymentScheduleState [redacted]";
}
