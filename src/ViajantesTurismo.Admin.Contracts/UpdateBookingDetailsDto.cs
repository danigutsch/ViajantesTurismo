using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// DTO for updating booking details (room type, bikes, companion) after creation.
/// </summary>
public sealed class UpdateBookingDetailsDto : IValidatableObject
{
    /// <summary>The room type for the booking.</summary>
    [Required]
    public required RoomTypeDto RoomType { get; init; }

    /// <summary>The bike type for the principal customer.</summary>
    [Required]
    public required BikeTypeDto PrincipalBikeType { get; init; }

    /// <summary>The companion customer ID (null to remove companion).</summary>
    public long? CompanionCustomerId { get; init; }

    /// <summary>The bike type for the companion (required if companion present).</summary>
    public BikeTypeDto? CompanionBikeType { get; init; }

    /// <summary>
    /// Validates the booking details based on business rules.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsSingleRoomAndHasCompanionSelected())
        {
            yield return new ValidationResult(
                "Single room bookings cannot have a companion. Please select Double Room or remove the companion.",
                [nameof(CompanionCustomerId)]);
        }

        if (CompanionHasNoBikeTypeSelected())
        {
            yield return new ValidationResult(
                "Companion bike type is required when a companion is selected.",
                [nameof(CompanionBikeType)]);
        }

        if (BikeTypeIsNone())
        {
            yield return new ValidationResult(
                "Principal customer must select a bike type (Regular or E-Bike).",
                [nameof(PrincipalBikeType)]);
        }

        if (CompanionBikeTypeIsNone())
        {
            yield return new ValidationResult(
                "Companion must select a bike type (Regular or E-Bike).",
                [nameof(CompanionBikeType)]);
        }
    }

    private bool CompanionBikeTypeIsNone() => CompanionBikeType is BikeTypeDto.None;
    private bool BikeTypeIsNone() => PrincipalBikeType == BikeTypeDto.None;
    private bool CompanionHasNoBikeTypeSelected() => CompanionCustomerId.HasValue && !CompanionBikeType.HasValue;
    private bool IsSingleRoomAndHasCompanionSelected() => RoomType == RoomTypeDto.SingleRoom && CompanionCustomerId.HasValue;
}