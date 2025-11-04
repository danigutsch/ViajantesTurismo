using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Web.Components.Shared;

/// <summary>
/// Form model for creating and editing bookings.
/// </summary>
public class BookingFormModel : IValidatableObject
{
    /// <summary>Tour identifier (required when creating from customer view).</summary>
    public int? TourId { get; set; }

    /// <summary>Customer identifier (required when creating from tour view).</summary>
    public int? CustomerId { get; set; }

    /// <summary>Optional companion customer identifier.</summary>
    public int? CompanionId { get; set; }

    /// <summary>Room type for the booking.</summary>
    [Required(ErrorMessage = "Room type is required")]
    public RoomTypeDto RoomType { get; set; } = RoomTypeDto.SingleRoom;

    /// <summary>Bike type for the principal customer.</summary>
    [Required(ErrorMessage = "Bike type is required for principal customer")]
    public BikeTypeDto PrincipalBikeType { get; set; } = BikeTypeDto.None;

    /// <summary>Bike type for the companion customer (if applicable).</summary>
    public BikeTypeDto? CompanionBikeType { get; set; }

    /// <summary>Optional notes about the booking.</summary>
    [MaxLength(ContractConstants.MaxBookingNotesLength, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }

    /// <summary>Booking status.</summary>
    public BookingStatusDto Status { get; set; } = BookingStatusDto.Pending;

    /// <summary>Payment status.</summary>
    public PaymentStatusDto PaymentStatus { get; set; } = PaymentStatusDto.Unpaid;

    /// <summary>Discount type applied to the booking.</summary>
    public DiscountTypeDto DiscountType { get; set; } = DiscountTypeDto.None;

    /// <summary>Discount amount (percentage 0-100 or absolute value).</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be positive")]
    public decimal DiscountAmount { get; set; }

    /// <summary>Reason for applying the discount.</summary>
    [StringLength(ContractConstants.MaxDiscountReasonLength, MinimumLength = ContractConstants.MinDiscountReasonLength, ErrorMessage = "Discount reason must be between {2} and {1} characters")]
    public string DiscountReason { get; set; } = string.Empty;

    /// <summary>
    /// Validates the discount fields based on business rules.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiscountType != DiscountTypeDto.None)
        {
            if (DiscountAmount <= 0)
            {
                yield return new ValidationResult("Discount amount must be greater than 0 when a discount is applied", new[] { nameof(DiscountAmount) });
            }

            if (DiscountType == DiscountTypeDto.Percentage && DiscountAmount > ContractConstants.MaxDiscountPercentage)
            {
                yield return new ValidationResult($"Percentage discount cannot exceed {ContractConstants.MaxDiscountPercentage}%", new[] { nameof(DiscountAmount) });
            }

            if (string.IsNullOrWhiteSpace(DiscountReason))
            {
                yield return new ValidationResult("Discount reason is required when applying a discount", new[] { nameof(DiscountReason) });
            }
        }
    }
}
