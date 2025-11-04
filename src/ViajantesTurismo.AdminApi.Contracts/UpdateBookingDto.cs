using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// DTO for updating an existing booking.
/// </summary>
public sealed class UpdateBookingDto : IValidatableObject
{
    /// <summary>Optional notes about the booking.</summary>
    [MaxLength(ContractConstants.MaxBookingNotesLength)]
    public string? Notes { get; init; }

    /// <summary>The booking status.</summary>
    [Required]
    public required BookingStatusDto Status { get; init; }

    /// <summary>The payment status.</summary>
    [Required]
    public required PaymentStatusDto PaymentStatus { get; init; }

    /// <summary>The discount type (None, Percentage, or Absolute).</summary>
    public DiscountTypeDto DiscountType { get; init; } = DiscountTypeDto.None;

    /// <summary>The discount amount (0-100 for percentage, fixed amount for absolute).</summary>
    public decimal DiscountAmount { get; init; }

    /// <summary>Reason for the discount (required when discount is applied).</summary>
    [StringLength(ContractConstants.MaxDiscountReasonLength, MinimumLength = ContractConstants.MinDiscountReasonLength)]
    public string? DiscountReason { get; init; }

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