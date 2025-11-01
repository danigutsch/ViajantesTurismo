using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Bookings;

/// <summary>
/// Provides predefined booking-related error results.
/// </summary>
public static class BookingErrors
{
    /// <summary>
    /// Indicates that a status transition is invalid for the current booking state.
    /// </summary>
    /// <param name="currentStatus">The current booking status.</param>
    /// <param name="targetStatus">The target booking status.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidStatusTransition(BookingStatus currentStatus, BookingStatus targetStatus) => 
        Result.Conflict(detail: $"Cannot transition from {currentStatus} to {targetStatus}.");
}
