using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests DraftBookingPaymentQuote with authenticated actor metadata and stable operation ownership.</summary>
public sealed record DraftBookingPaymentQuoteCommand(Guid BookingId, CreatePaymentQuoteDto Input, string ActorId, string? IdempotencyKey)
{
    /// <inheritdoc />
    public override string ToString() => "DraftBookingPaymentQuoteCommand [redacted]";
}
