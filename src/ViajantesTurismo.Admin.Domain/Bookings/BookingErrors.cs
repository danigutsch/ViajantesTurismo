using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Bookings;

/// <summary>
/// Provides predefined booking-related error results.
/// </summary>
public static class BookingErrors
{
    /// <summary>
    /// Indicates that a cancelled booking cannot be confirmed.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result CannotConfirmCancelledBooking() => Result.Conflict(
        detail: "Cannot confirm a cancelled booking.");
}