namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record BookingQuoteState(Guid Id, decimal BrlPerEuro, string Source, string Reference, DateTimeOffset AgreedAt, DateTimeOffset ExpiresAt, bool ForRefund, string CreatedBy, DateTimeOffset CreatedAt, FinancialApprovalState? Approval)
{
    public override string ToString() => "BookingQuoteState [redacted]";
}
