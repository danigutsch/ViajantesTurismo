using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// DTO for updating booking details (room type, bikes, companion) after creation.
/// </summary>
public sealed record UpdateBookingDetailsDto : IValidatableObject
{
    /// <summary>The room type for the booking.</summary>
    [Required]
    public required RoomTypeDto RoomType { get; init; }

    /// <summary>The bike type for the principal customer.</summary>
    [Required]
    public required BikeTypeDto PrincipalBikeType { get; init; }

    /// <summary>The companion customer ID (null to remove companion).</summary>
    public Guid? CompanionCustomerId { get; init; }

    /// <summary>The bike type for the companion (required if companion present).</summary>
    public BikeTypeDto? CompanionBikeType { get; init; }

    /// <summary>
    /// Validates the booking details based on business rules.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var singleRoomResult = BookingValidation.ValidateSingleRoomNoCompanion(
            RoomType,
            CompanionCustomerId,
            nameof(CompanionCustomerId));

        if (singleRoomResult is not null)
        {
            yield return singleRoomResult;
        }

        var companionBikeTypeResult = BookingValidation.ValidateCompanionHasBikeType(
            CompanionCustomerId,
            CompanionBikeType,
            nameof(CompanionBikeType));

        if (companionBikeTypeResult is not null)
        {
            yield return companionBikeTypeResult;
        }

        var principalBikeTypeResult = BookingValidation.ValidatePrincipalBikeType(
            PrincipalBikeType,
            nameof(PrincipalBikeType));

        if (principalBikeTypeResult is not null)
        {
            yield return principalBikeTypeResult;
        }

        var companionBikeTypeNoneResult = BookingValidation.ValidateCompanionBikeTypeNotNone(
            CompanionBikeType,
            nameof(CompanionBikeType));

        if (companionBikeTypeNoneResult is not null)
        {
            yield return companionBikeTypeNoneResult;
        }
    }
}
