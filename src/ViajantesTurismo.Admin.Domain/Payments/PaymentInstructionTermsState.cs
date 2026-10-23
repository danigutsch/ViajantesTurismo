using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record PaymentInstructionTermsState(
    Guid BookingId, Guid ScheduleId, Guid AccountId, Guid AccountRevisionId, Currency Currency,
    Guid? QuoteId, BookingQuoteState? Quote, PaymentInstructionInstallmentState[] Installments,
    decimal EuroAmount, decimal SettlementAmount, string Reference, DateTimeOffset CreatedAt);
