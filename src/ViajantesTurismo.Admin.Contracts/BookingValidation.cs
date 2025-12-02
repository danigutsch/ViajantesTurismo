using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Provides centralised booking validation for DTO validation.
/// </summary>
public static class BookingValidation
{
    /// <summary>
    /// Validates that a single room booking does not have a companion.
    /// </summary>
    /// <param name="roomType">The room type.</param>
    /// <param name="companionCustomerId">The companion customer ID, if any.</param>
    /// <param name="companionMemberName">The name of the companion property.</param>
    /// <returns>A validation result if invalid, or null if valid.</returns>
    public static ValidationResult? ValidateSingleRoomNoCompanion(
        RoomTypeDto roomType,
        Guid? companionCustomerId,
        string companionMemberName)
    {
        return roomType == RoomTypeDto.SingleRoom && companionCustomerId.HasValue
            ? new ValidationResult(
                "Single room bookings cannot have a companion. Please select Double Room or remove the companion.",
                [companionMemberName])
            : null;
    }

    /// <summary>
    /// Validates that a companion has a bike type selected.
    /// </summary>
    /// <param name="companionCustomerId">The companion customer ID, if any.</param>
    /// <param name="companionBikeType">The companion bike type, if any.</param>
    /// <param name="companionBikeTypeMemberName">The name of the companion bike type property.</param>
    /// <returns>A validation result if invalid, or null if valid.</returns>
    public static ValidationResult? ValidateCompanionHasBikeType(
        Guid? companionCustomerId,
        BikeTypeDto? companionBikeType,
        string companionBikeTypeMemberName)
    {
        return companionCustomerId.HasValue && !companionBikeType.HasValue
            ? new ValidationResult(
                "Companion bike type is required when a companion is selected.",
                [companionBikeTypeMemberName])
            : null;
    }

    /// <summary>
    /// Validates that the principal customer has a valid bike type (not None).
    /// </summary>
    /// <param name="principalBikeType">The principal bike type.</param>
    /// <param name="principalBikeTypeMemberName">The name of the principal bike type property.</param>
    /// <returns>A validation result if invalid, or null if valid.</returns>
    public static ValidationResult? ValidatePrincipalBikeType(
        BikeTypeDto principalBikeType,
        string principalBikeTypeMemberName)
    {
        return principalBikeType == BikeTypeDto.None
            ? new ValidationResult(
                "Principal customer must select a bike type (Regular or E-Bike).",
                [principalBikeTypeMemberName])
            : null;
    }

    /// <summary>
    /// Validates that the companion has a valid bike type (not None) when present.
    /// </summary>
    /// <param name="companionBikeType">The companion bike type, if any.</param>
    /// <param name="companionBikeTypeMemberName">The name of the companion bike type property.</param>
    /// <returns>A validation result if invalid, or null if valid.</returns>
    public static ValidationResult? ValidateCompanionBikeTypeNotNone(
        BikeTypeDto? companionBikeType,
        string companionBikeTypeMemberName)
    {
        return companionBikeType == BikeTypeDto.None
            ? new ValidationResult(
                "Companion must select a bike type (Regular or E-Bike).",
                [companionBikeTypeMemberName])
            : null;
    }
}
