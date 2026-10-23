using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests ApproveBookingPaymentQuote with authenticated actor metadata and stable operation ownership.</summary>
public sealed record ApproveBookingPaymentQuoteCommand(Guid BookingId, Guid QuoteId, ApproveFinancialRecordDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "ApproveBookingPaymentQuoteCommand [redacted]";
}
