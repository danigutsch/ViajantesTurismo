using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted payment receipt.</summary>
/// <param name="Id">Receipt identifier.</param>
/// <param name="Currency">Currency of the payment.</param>
/// <param name="Gross">Gross amount paid.</param>
/// <param name="Fees">Fees deducted from the gross amount.</param>
/// <param name="PaymentDate">Civil calendar date of the payment.</param>
/// <param name="SettledAt">UTC settlement instant; null while unsettled.</param>
/// <param name="Method">Payment method.</param>
/// <param name="Reference">Payment reference.</param>
/// <param name="CreatedBy">Actor who recorded the receipt.</param>
/// <param name="CreatedAt">UTC recording instant.</param>
/// <param name="ProposedAllocations">EUR allocations proposed when the receipt was recorded.</param>
internal sealed record PaymentReceiptState(Guid Id, Currency Currency, decimal Gross, decimal Fees, DateOnly PaymentDate, DateTimeOffset? SettledAt, PaymentMethod Method, string Reference, string CreatedBy, DateTimeOffset CreatedAt, PaymentSelectionState[] ProposedAllocations)
{
    public override string ToString() => "PaymentReceiptState [redacted]";
}
