using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// DTO for creating a new booking.
/// </summary>
public sealed record CreateBookingDto : IValidatableObject
{
    /// <summary>The ID of the tour being booked.</summary>
    [Required]
    public required Guid TourId { get; init; }

    /// <summary>The ID of the principal customer making the booking.</summary>
    [Required]
    public required Guid PrincipalCustomerId { get; init; }

    /// <summary>The bike type selected by the principal customer.</summary>
    [Required]
    public required BikeTypeDto PrincipalBikeType { get; init; }

    /// <summary>The ID of the companion customer, if any.</summary>
    public Guid? CompanionCustomerId { get; init; }

    /// <summary>The bike type selected by the companion, if any.</summary>
    public BikeTypeDto? CompanionBikeType { get; init; }

    /// <summary>The room type for the booking.</summary>
    [Required]
    public required RoomTypeDto RoomType { get; init; }

    /// <summary>The discount type (None, Percentage, or Absolute).</summary>
    public DiscountTypeDto DiscountType { get; init; } = DiscountTypeDto.None;

    /// <summary>The discount amount (0-100 for percentage, fixed amount for absolute).</summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>Reason for the discount (required when discount is applied).</summary>
    [StringLength(ContractConstants.MaxDiscountReasonLength, MinimumLength = ContractConstants.MinDiscountReasonLength)]
    public string? DiscountReason { get; init; }

    /// <summary>Optional notes about the booking.</summary>
    [StringLength(ContractConstants.MaxBookingNotesLength)]
    public string? Notes { get; init; }

    /// <summary>
    /// Validates the discount fields based on business rules.
    /// Returns multiple validation errors when multiple fields are invalid.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in DiscountValidation.Validate(
                     DiscountType,
                     DiscountAmount,
                     DiscountReason,
                     ContractConstants.MaxDiscountPercentage,
                     ContractConstants.MinDiscountReasonLength,
                     nameof(DiscountAmount),
                     nameof(DiscountReason)))
        {
            yield return result;
        }
    }
}
