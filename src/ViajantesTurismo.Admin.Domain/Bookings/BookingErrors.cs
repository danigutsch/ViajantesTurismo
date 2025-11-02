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
    public static Result ZeroOrNegativePrice(decimal price) =>
        Result.Invalid(
            detail: $"Price must be greater than zero. Received: {price}.",
            field: "price",
            message: "Price must be greater than zero.");

    /// <summary>
    /// Indicates that the price is invalid (must be greater than zero).
    /// </summary>
    /// <param name="price">The invalid price value.</param>
    /// <param name="maxPrice">The maximum allowed price.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result PriceExceedsMaximum(decimal price, decimal maxPrice) =>
        Result.Invalid(
            detail: $"Price must be less than {maxPrice}. Received: {price}.",
            field: "price",
            message: $"Price must be less than {maxPrice}.");

    /// <summary>
    /// Indicates that the notes exceed the maximum allowed length.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="actualLength">The actual length provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidNotesLength(int maxLength, int actualLength) =>
        Result.Invalid(
            detail: $"Notes cannot exceed {maxLength} characters. Received: {actualLength} characters.",
            field: "notes",
            message: $"Notes cannot exceed {maxLength} characters.");

    /// <summary>
    /// Indicates that a booking with the specified ID was not found.
    /// </summary>
    /// <param name="id">The ID of the booking that was not found.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result BookingNotFound(long id) => Result.NotFound(detail: $"Booking with ID {id} was not found.");
}
