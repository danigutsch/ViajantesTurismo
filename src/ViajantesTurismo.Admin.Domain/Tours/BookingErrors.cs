using ViajantesTurismo.Admin.Domain.Shared;
using PandaBear.Results;

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
    /// Indicates that a booking cannot be completed without first being confirmed.
    /// </summary>
    /// <returns>A Result representing the validation error.</returns>
    public static Result CannotCompleteWithoutConfirmation() =>
        Result.Invalid(
            detail: "Booking must be confirmed before it can be completed.",
            field: "status",
            message: "Booking must be confirmed first.");

    /// <summary>
    /// Indicates that the base price is zero or negative.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result ZeroOrNegativeBasePrice() =>
        Result.Invalid(
            detail: "Base price must be greater than zero.",
            field: "basePrice",
            message: "Base price must be greater than zero.");

    /// <summary>
    /// Indicates that the base price exceeds the maximum allowed value.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result BasePriceExceedsMaximum() =>
        Result.Invalid(
            detail: "Base price exceeds the maximum allowed value.",
            field: "basePrice",
            message: "Base price exceeds the maximum allowed value.");

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
    /// <returns>A Result representing the error.</returns>
    public static Result BookingNotFound() => Result.NotFound(detail: "Booking was not found.");

    /// <summary>
    /// Indicates that the bike price is negative.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result NegativeBikePrice() =>
        Result.Invalid(
            detail: "Bike price cannot be negative.",
            field: "bikePrice",
            message: "Bike price cannot be negative.");

    /// <summary>
    /// Indicates that the bike price exceeds the maximum allowed value.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result BikePriceExceedsMaximum() =>
        Result.Invalid(
            detail: "Bike price exceeds the maximum allowed value.",
            field: "bikePrice",
            message: "Bike price exceeds the maximum allowed value.");

    /// <summary>
    /// Indicates that the room additional cost is negative.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result NegativeRoomCost() =>
        Result.Invalid(
            detail: "Room additional cost cannot be negative.",
            field: "roomAdditionalCost",
            message: "Room additional cost cannot be negative.");

    /// <summary>
    /// Indicates that the room additional cost exceeds the maximum allowed value.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result RoomCostExceedsMaximum() =>
        Result.Invalid(
            detail: "Room additional cost exceeds the maximum allowed value.",
            field: "roomAdditionalCost",
            message: "Room additional cost exceeds the maximum allowed value.");

    /// <summary>
    /// Indicates that an invalid bike type value was provided.
    /// </summary>
    /// <param name="bikeType">The invalid bike type value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidBikeType(BikeType bikeType) =>
        Result.Invalid(
            detail: $"Invalid bike type: {bikeType}. Valid values are: {string.Join(", ", Enum.GetNames<BikeType>())}.",
            field: "bikeType",
            message: $"Invalid bike type: {bikeType}.");

    /// <summary>
    /// Indicates that bike type must be selected (None is not allowed).
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result BikeTypeNotSelected() =>
        Result.Invalid(
            detail: "Bike type must be selected. Please choose Regular or EBike.",
            field: "bikeType",
            message: "Bike type must be selected.");

    /// <summary>
    /// Indicates that an invalid room type value was provided.
    /// </summary>
    /// <param name="roomType">The invalid room type value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidRoomType(RoomType roomType) =>
        Result.Invalid(
            detail: $"Invalid room type: {roomType}. Valid values are: {string.Join(", ", Enum.GetNames<RoomType>())}.",
            field: "roomType",
            message: $"Invalid room type: {roomType}.");

    /// <summary>
    /// Indicates that a cancelled or completed booking cannot be modified.
    /// </summary>
    /// <param name="status">The current booking status.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result CannotModifyCancelledOrCompletedBooking(BookingStatus status) =>
        Result.Conflict(
            detail: $"This booking is {status} and cannot be modified.");

    /// <summary>
    /// Indicates that the principal and companion customers cannot be the same person.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result PrincipalAndCompanionCannotBeSame() =>
        Result.Invalid(
            detail: "Principal and companion customers cannot be the same person.",
            field: "companionCustomerId",
            message: "Principal and companion customers cannot be the same person.");

    /// <summary>
    /// Indicates that a single room cannot have a companion.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result SingleRoomCannotHaveCompanion() =>
        Result.Invalid(
            detail: "Single room bookings cannot have a companion. Please select double room for companion bookings.",
            field: "companionCustomerId",
            message: "Single room cannot have a companion.");

    /// <summary>
    /// Indicates that companion bike type is required when a companion is present.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result CompanionBikeTypeRequired() =>
        Result.Invalid(
            detail: "Companion bike type is required when companion is present.",
            field: "companionBikeType",
            message: "Companion bike type is required.");

    /// <summary>
    /// Indicates that companion bike type cannot be specified without a companion.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result CompanionBikeWithoutCompanion() =>
        Result.Invalid(
            detail: "Companion bike type cannot be specified without a companion customer.",
            field: "companionBikeType",
            message: "Companion bike requires a companion customer.");
}
