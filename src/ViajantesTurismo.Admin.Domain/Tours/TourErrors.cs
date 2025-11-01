using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Provides predefined tour-related error results.
/// </summary>
public static class TourErrors
{
    /// <summary>
    /// Indicates that a booking was not found in the tour.
    /// </summary>
    /// <param name="bookingId">The ID of the booking that was not found.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result BookingNotFound(long bookingId) => Result.NotFound(
        detail: $"Booking with ID {bookingId} not found in this tour.");
}