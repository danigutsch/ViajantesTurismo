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

    /// <summary>
    /// Indicates that the price is invalid (must be greater than zero).
    /// </summary>
    /// <param name="price">The invalid price value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidPrice(decimal price) => 
        Result.Invalid(detail: $"Price must be greater than zero. Received: {price}.");

    /// <summary>
    /// Indicates that the notes exceed the maximum allowed length.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="actualLength">The actual length provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidNotesLength(int maxLength, int actualLength) => 
        Result.Invalid(detail: $"Notes cannot exceed {maxLength} characters. Received: {actualLength} characters.");
}