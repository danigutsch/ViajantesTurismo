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

    /// <summary>
    /// Indicates that the tour identifier is empty or whitespace.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> EmptyIdentifier() => Result<Tour>.Invalid(
        detail: "Tour identifier cannot be empty or whitespace.",
        field: "identifier",
        message: "Identifier is required.");

    /// <summary>
    /// Indicates that the tour identifier exceeds the maximum allowed length.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="actualLength">The actual length provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> IdentifierTooLong(int maxLength, int actualLength) => Result<Tour>.Invalid(
        detail: $"Tour identifier cannot exceed {maxLength} characters. Received: {actualLength} characters.",
        field: "identifier",
        message: $"Cannot exceed {maxLength} characters.");

    /// <summary>
    /// Indicates that the tour name is empty or whitespace.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> EmptyName() => Result<Tour>.Invalid(
        detail: "Tour name cannot be empty or whitespace.",
        field: "name",
        message: "Name is required.");

    /// <summary>
    /// Indicates that the tour name exceeds the maximum allowed length.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="actualLength">The actual length provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> NameTooLong(int maxLength, int actualLength) => Result<Tour>.Invalid(
        detail: $"Tour name cannot exceed {maxLength} characters. Received: {actualLength} characters.",
        field: "name",
        message: $"Cannot exceed {maxLength} characters.");

    /// <summary>
    /// Indicates that the tour duration is too short.
    /// </summary>
    /// <param name="minimumDays">The minimum required duration in days.</param>
    /// <param name="actualDays">The actual duration provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> DurationTooShort(int minimumDays, double actualDays) => Result<Tour>.Invalid(
        detail: $"Tour must be at least {minimumDays} days long. Received: {actualDays:F1} days.",
        field: "schedule",
        message: $"Duration must be at least {minimumDays} days.");

    /// <summary>
    /// Indicates that the tour duration is too short (non-generic version for update operations).
    /// </summary>
    /// <param name="minimumDays">The minimum required duration in days.</param>
    /// <param name="actualDays">The actual duration provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result DurationTooShortUpdate(int minimumDays, double actualDays) => Result.Invalid(
        detail: $"Tour must be at least {minimumDays} days long. Received: {actualDays:F1} days.",
        field: "schedule",
        message: $"Duration must be at least {minimumDays} days.");

    /// <summary>
    /// Indicates that the end date is not after the start date.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> InvalidDateRange() => Result<Tour>.Invalid(
        detail: "End date must be after start date.",
        field: "schedule",
        message: "End date must be after start date.");

    /// <summary>
    /// Indicates that the end date is not after the start date (non-generic version for update operations).
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidDateRangeUpdate() => Result.Invalid(
        detail: "End date must be after start date.",
        field: "schedule",
        message: "End date must be after start date.");

    /// <summary>
    /// Indicates that a price value is invalid (negative).
    /// </summary>
    /// <param name="priceType">The type of price (e.g., "Base price", "Single room supplement").</param>
    /// <param name="value">The invalid price value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> InvalidPrice(string priceType, decimal value) => Result<Tour>.Invalid(
        detail: $"{priceType} must be greater than or equal to zero. Received: {value}.",
        field: "price",
        message: $"{priceType} must be greater than or equal to zero.");

    /// <summary>
    /// Indicates that a price value exceeds the maximum allowed value.
    /// </summary>
    /// <param name="priceType">The type of price (e.g., "Base price", "Single room supplement").</param>
    /// <param name="maxPrice">The maximum allowed price.</param>
    /// <param name="value">The actual price value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> PriceTooHigh(string priceType, decimal maxPrice, decimal value) => Result<Tour>.Invalid(
        detail: $"{priceType} cannot exceed {maxPrice}. Received: {value}.",
        field: "price",
        message: $"{priceType} cannot exceed {maxPrice}.");

    /// <summary>
    /// Indicates that a tour with the specified ID was not found.
    /// </summary>
    /// <param name="id">The ID of the tour that was not found.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> TourNotFound(int id) => Result<Tour>.NotFound(detail: $"Tour with ID {id} was not found.");

    /// <summary>
    /// Indicates that the principal customer and companion cannot be the same person.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result PrincipalAndCompanionCannotBeSame() => Result.Invalid(
        detail: "Principal customer and companion cannot be the same person.",
        field: "companionCustomerId",
        message: "Principal and companion must be different customers.");

    /// <summary>
    /// Indicates that MinCustomers must be a positive integer.
    /// </summary>
    /// <param name="value">The invalid value provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> InvalidMinCustomers(int value) => Result<Tour>.Invalid(
        detail: $"Minimum customers must be between 1 and 20. Received: {value}.",
        field: "minCustomers",
        message: "Minimum customers must be between 1 and 20.");

    /// <summary>
    /// Indicates that MaxCustomers must be a positive integer.
    /// </summary>
    /// <param name="value">The invalid value provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> InvalidMaxCustomers(int value) => Result<Tour>.Invalid(
        detail: $"Maximum customers must be between 1 and 20. Received: {value}.",
        field: "maxCustomers",
        message: "Maximum customers must be between 1 and 20.");

    /// <summary>
    /// Indicates that MaxCustomers must be greater than or equal to MinCustomers.
    /// </summary>
    /// <param name="minCustomers">The minimum customers value.</param>
    /// <param name="maxCustomers">The maximum customers value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result<Tour> MaxCustomersLessThanMin(int minCustomers, int maxCustomers) => Result<Tour>.Invalid(
        detail: $"Maximum customers ({maxCustomers}) must be greater than or equal to minimum customers ({minCustomers}).",
        field: "maxCustomers",
        message: $"Maximum must be >= minimum ({minCustomers}).");

    /// <summary>
    /// Indicates that the tour is fully booked and cannot accept more bookings.
    /// </summary>
    /// <param name="maxCustomers">The maximum capacity.</param>
    /// <param name="currentCount">The current number of customers.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result TourFullyBooked(int maxCustomers, int currentCount) => Result.Conflict(
        detail: $"Tour is fully booked. Maximum capacity: {maxCustomers}, Current bookings: {currentCount}. Cannot accept more bookings.");

    /// <summary>
    /// Indicates that MinCustomers must be a positive integer (update operation).
    /// </summary>
    /// <param name="value">The invalid value provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidMinCustomersUpdate(int value) => Result.Invalid(
        detail: $"Minimum customers must be between 1 and 20. Received: {value}.",
        field: "minCustomers",
        message: "Minimum customers must be between 1 and 20.");

    /// <summary>
    /// Indicates that MaxCustomers must be a positive integer (update operation).
    /// </summary>
    /// <param name="value">The invalid value provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidMaxCustomersUpdate(int value) => Result.Invalid(
        detail: $"Maximum customers must be between 1 and 20. Received: {value}.",
        field: "maxCustomers",
        message: "Maximum customers must be between 1 and 20.");

    /// <summary>
    /// Indicates that MaxCustomers must be greater than or equal to MinCustomers (update operation).
    /// </summary>
    /// <param name="minCustomers">The minimum customers value.</param>
    /// <param name="maxCustomers">The maximum customers value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result MaxCustomersLessThanMinUpdate(int minCustomers, int maxCustomers) => Result.Invalid(
        detail: $"Maximum customers ({maxCustomers}) must be greater than or equal to minimum customers ({minCustomers}).",
        field: "maxCustomers",
        message: $"Maximum must be >= minimum ({minCustomers}).");
}
