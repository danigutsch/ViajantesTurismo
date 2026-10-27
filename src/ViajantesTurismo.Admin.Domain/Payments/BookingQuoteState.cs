namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted agreed BRL per EUR quote and its approval.</summary>
/// <param name="Id">Quote identifier.</param>
/// <param name="BrlPerEuro">Agreed BRL amount per one EUR.</param>
/// <param name="Source">Where the rate was agreed.</param>
/// <param name="Reference">External reference of the agreement.</param>
/// <param name="AgreedAt">UTC instant the rate was agreed.</param>
/// <param name="ExpiresAt">Exclusive UTC expiry instant of the quote.</param>
/// <param name="ForRefund">Whether the quote is for refunds rather than payment instructions.</param>
/// <param name="CreatedBy">Actor who drafted the quote.</param>
/// <param name="CreatedAt">UTC instant the quote was drafted.</param>
/// <param name="Approval">Approval of the quote; null while it is a draft.</param>
internal sealed record BookingQuoteState(Guid Id, decimal BrlPerEuro, string Source, string Reference, DateTimeOffset AgreedAt, DateTimeOffset ExpiresAt, bool ForRefund, string CreatedBy, DateTimeOffset CreatedAt, FinancialApprovalState? Approval)
{
    public override string ToString() => "BookingQuoteState [redacted]";
}
