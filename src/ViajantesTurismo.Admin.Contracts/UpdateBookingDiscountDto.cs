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
