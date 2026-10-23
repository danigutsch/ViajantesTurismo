using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentReceiptState(Guid Id, Currency Currency, decimal Gross, decimal Fees, DateOnly PaymentDate, DateTimeOffset? SettledAt, PaymentMethod Method, string Reference, string CreatedBy, DateTimeOffset CreatedAt, PaymentSelectionState[] ProposedAllocations)
{
    public override string ToString() => "PaymentReceiptState [redacted]";
}
