using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted terms frozen into a payment instruction.</summary>
/// <param name="BookingId">Identifier of the booking.</param>
/// <param name="ScheduleId">Identifier of the approved schedule revision used.</param>
/// <param name="AccountId">Identifier of the payment account.</param>
/// <param name="AccountRevisionId">Identifier of the account revision used.</param>
/// <param name="Currency">Settlement currency.</param>
/// <param name="QuoteId">Identifier of the approved quote; null for EUR settlement.</param>
/// <param name="Quote">Frozen copy of the quote; null for EUR settlement.</param>
/// <param name="Installments">Selected installments with settlement amounts.</param>
/// <param name="EuroAmount">Total EUR amount covered.</param>
/// <param name="SettlementAmount">Total settlement-currency amount payable.</param>
/// <param name="Reference">Payment reference.</param>
/// <param name="CreatedAt">UTC creation instant.</param>
internal sealed record PaymentInstructionTermsState(
    Guid BookingId, Guid ScheduleId, Guid AccountId, Guid AccountRevisionId, Currency Currency,
    Guid? QuoteId, BookingQuoteState? Quote, PaymentInstructionInstallmentState[] Installments,
    decimal EuroAmount, decimal SettlementAmount, string Reference, DateTimeOffset CreatedAt);
