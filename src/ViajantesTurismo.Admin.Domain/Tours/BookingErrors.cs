using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

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
    /// Indicates that the base price is zero or negative.
    /// </summary>
    /// <param name="price">The invalid base price value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result ZeroOrNegativeBasePrice(decimal price) =>
        Result.Invalid(
            detail: $"Base price must be greater than zero. Received: {price}.",
            field: "basePrice",
            message: "Base price must be greater than zero.");

    /// <summary>
    /// Indicates that the base price exceeds the maximum allowed value.
    /// </summary>
    /// <param name="price">The invalid base price value.</param>
    /// <param name="maxPrice">The maximum allowed base price.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result BasePriceExceedsMaximum(decimal price, decimal maxPrice) =>
        Result.Invalid(
            detail: $"Base price must be less than {maxPrice}. Received: {price}.",
            field: "basePrice",
            message: $"Base price must be less than {maxPrice}.");

    /// <summary>
    /// Indicates that the total price is zero or negative.
    /// </summary>
    /// <param name="price">The invalid total price value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result ZeroOrNegativeTotalPrice(decimal price) =>
        Result.Invalid(
            detail: $"Total price must be greater than zero. Received: {price}.",
            field: "totalPrice",
            message: "Total price must be greater than zero.");

    /// <summary>
    /// Indicates that the total price exceeds the maximum allowed value.
    /// </summary>
    /// <param name="price">The invalid total price value.</param>
    /// <param name="maxPrice">The maximum allowed total price.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result TotalPriceExceedsMaximum(decimal price, decimal maxPrice) =>
        Result.Invalid(
            detail: $"Total price must be less than {maxPrice}. Received: {price}.",
            field: "totalPrice",
            message: $"Total price must be less than {maxPrice}.");

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

    /// <summary>
    /// Indicates that the bike price is negative.
    /// </summary>
    /// <param name="price">The invalid bike price value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result NegativeBikePrice(decimal price) =>
        Result.Invalid(
            detail: $"Bike price cannot be negative. Received: {price}.",
            field: "bikePrice",
            message: "Bike price cannot be negative.");

    /// <summary>
    /// Indicates that the bike price exceeds the maximum allowed value.
    /// </summary>
    /// <param name="price">The invalid bike price value.</param>
    /// <param name="maxPrice">The maximum allowed bike price.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result BikePriceExceedsMaximum(decimal price, decimal maxPrice) =>
        Result.Invalid(
            detail: $"Bike price must be less than {maxPrice}. Received: {price}.",
            field: "bikePrice",
            message: $"Bike price must be less than {maxPrice}.");

    /// <summary>
    /// Indicates that the room additional cost is negative.
    /// </summary>
    /// <param name="cost">The invalid room cost value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result NegativeRoomCost(decimal cost) =>
        Result.Invalid(
            detail: $"Room additional cost cannot be negative. Received: {cost}.",
            field: "roomAdditionalCost",
            message: "Room additional cost cannot be negative.");

    /// <summary>
    /// Indicates that the room additional cost exceeds the maximum allowed value.
    /// </summary>
    /// <param name="cost">The invalid room cost value.</param>
    /// <param name="maxCost">The maximum allowed room cost.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result RoomCostExceedsMaximum(decimal cost, decimal maxCost) =>
        Result.Invalid(
            detail: $"Room additional cost must be less than {maxCost}. Received: {cost}.",
            field: "roomAdditionalCost",
            message: $"Room additional cost must be less than {maxCost}.");
}