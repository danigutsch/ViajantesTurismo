using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// DTO for creating a new booking.
/// </summary>
public sealed class CreateBookingDto : IValidatableObject
{
    /// <summary>The ID of the tour being booked.</summary>
    [Required]
    public required int TourId { get; init; }

    /// <summary>The ID of the principal customer making the booking.</summary>
    [Required]
    public required int PrincipalCustomerId { get; init; }

    /// <summary>The bike type selected by the principal customer.</summary>
    [Required]
    public required BikeTypeDto PrincipalBikeType { get; init; }

    /// <summary>The ID of the companion customer, if any.</summary>
    public int? CompanionCustomerId { get; init; }

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
    [MaxLength(ContractConstants.MaxBookingNotesLength)]
    public string? Notes { get; init; }

    /// <summary>
    /// Validates the discount fields based on business rules.
    /// Returns multiple validation errors when multiple fields are invalid.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountType != DiscountTypeDto.None)
        {
            if (DiscountAmount <= 0)
            {
                yield return DiscountErrors.AmountMustBePositive();
            }

            if (DiscountType == DiscountTypeDto.Percentage && DiscountAmount > ContractConstants.MaxDiscountPercentage)
            {
                yield return DiscountErrors.PercentageTooHigh(ContractConstants.MaxDiscountPercentage);
            }

            if (string.IsNullOrWhiteSpace(DiscountReason))
            {
                yield return DiscountErrors.ReasonRequired();
            }
            else if (DiscountReason.Length < ContractConstants.MinDiscountReasonLength)
            {
                yield return DiscountErrors.ReasonTooShort(ContractConstants.MinDiscountReasonLength);
            }
        }
    }
}