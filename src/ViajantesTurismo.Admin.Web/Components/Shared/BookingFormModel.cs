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
    public RoomTypeDto RoomType { get; set; } = RoomTypeDto.DoubleOccupancy;

    [Required(ErrorMessage = "Bike type is required for principal customer")]
    public BikeTypeDto PrincipalBikeType { get; set; } = BikeTypeDto.None;

    public BikeTypeDto? CompanionBikeType { get; set; }

    [MaxLength(ContractConstants.MaxBookingNotesLength, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }

    public DiscountTypeDto DiscountType { get; set; } = DiscountTypeDto.None;

    [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be positive")]
    public decimal DiscountAmount { get; set; }

    [MaxLength(ContractConstants.MaxDiscountReasonLength, ErrorMessage = "Discount reason cannot exceed {1} characters")]
    public string DiscountReason { get; set; } = string.Empty;

    /// <summary>
    /// Validates discount fields using the same rules as CreateBookingDto.
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
