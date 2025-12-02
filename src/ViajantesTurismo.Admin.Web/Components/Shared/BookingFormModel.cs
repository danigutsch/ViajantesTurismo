using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Web.Components.Shared;

/// <summary>
/// Form model for creating bookings. Provides mutable properties for Blazor binding
/// and converts to immutable CreateBookingDto for API submission.
/// </summary>
public class BookingFormModel : IValidatableObject
{
    public Guid? TourId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? CompanionId { get; set; }

    [Required(ErrorMessage = "Room type is required")]
    public RoomTypeDto RoomType { get; set; } = RoomTypeDto.SingleRoom;

    [Required(ErrorMessage = "Bike type is required for principal customer")]
    public BikeTypeDto PrincipalBikeType { get; set; } = BikeTypeDto.None;

    public BikeTypeDto? CompanionBikeType { get; set; }

    [MaxLength(ContractConstants.MaxBookingNotesLength, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }

    public DiscountTypeDto DiscountType { get; set; } = DiscountTypeDto.None;

    [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be positive")]
    public decimal DiscountAmount { get; set; }

    [StringLength(ContractConstants.MaxDiscountReasonLength, MinimumLength = ContractConstants.MinDiscountReasonLength,
        ErrorMessage = "Discount reason must be between {2} and {1} characters")]
    public string DiscountReason { get; set; } = string.Empty;

    /// <summary>
    /// Validates discount fields using the same rules as CreateBookingDto.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountType != DiscountTypeDto.None)
        {
            if (DiscountAmount <= 0)
            {
                yield return DiscountValidation.AmountMustBePositive();
            }

            if (DiscountType == DiscountTypeDto.Percentage && DiscountAmount > ContractConstants.MaxDiscountPercentage)
            {
                yield return DiscountValidation.PercentageTooHigh(ContractConstants.MaxDiscountPercentage);
            }

            if (string.IsNullOrWhiteSpace(DiscountReason))
            {
                yield return DiscountValidation.ReasonRequired();
            }
            else if (DiscountReason.Length < ContractConstants.MinDiscountReasonLength)
            {
                yield return DiscountValidation.ReasonTooShort(ContractConstants.MinDiscountReasonLength);
            }
        }
    }

    /// <summary>
    /// Converts this form model to a CreateBookingDto for API submission.
    /// </summary>
    public CreateBookingDto ToDto() => new()
    {
        TourId = TourId!.Value,
        PrincipalCustomerId = CustomerId!.Value,
        PrincipalBikeType = PrincipalBikeType,
        CompanionCustomerId = CompanionId,
        CompanionBikeType = CompanionBikeType,
        RoomType = RoomType,
        Notes = Notes,
        DiscountType = DiscountType,
        DiscountAmount = DiscountAmount,
        DiscountReason = DiscountReason
    };
}
