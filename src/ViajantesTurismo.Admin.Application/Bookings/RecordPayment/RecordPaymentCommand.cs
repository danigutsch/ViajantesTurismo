using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Application.Bookings.RecordPayment;

/// <summary>
/// Command to record a payment for a booking.
/// </summary>
public sealed record RecordPaymentCommand(
    Guid BookingId,
    decimal Amount,
    DateTime PaymentDate,
    PaymentMethodDto Method,
    string? ReferenceNumber,
    string? Notes);
