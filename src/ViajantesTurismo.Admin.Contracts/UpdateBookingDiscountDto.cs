using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// DTO for updating booking discount after creation.
/// </summary>
public sealed record UpdateBookingDiscountDto : IValidatableObject
{
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
