using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Web.Components.Shared;

/// <summary>
/// The form model for creating and editing bookings.
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

    /// <summary>
    /// Calculates the subtotal price before discount.
    /// </summary>
    /// <param name="basePrice">Tour base price per person</param>
    /// <param name="doubleRoomSupplement">Double room supplement price</param>
    /// <param name="regularBikePrice">Regular bike price</param>
    /// <param name="eBikePrice">E-bike price</param>
    public decimal CalculateSubtotal(decimal basePrice, decimal doubleRoomSupplement, decimal regularBikePrice, decimal eBikePrice)
    {
        var customerCount = CompanionId.HasValue ? 2 : 1;
        var subtotal = basePrice * customerCount;

        if (RoomType == RoomTypeDto.DoubleRoom)
        {
            subtotal += doubleRoomSupplement;
        }

        subtotal += PrincipalBikeType switch
        {
            BikeTypeDto.Regular => regularBikePrice,
            BikeTypeDto.EBike => eBikePrice,
            _ => 0m
        };

        if (CompanionBikeType.HasValue)
        {
            subtotal += CompanionBikeType.Value switch
            {
                BikeTypeDto.Regular => regularBikePrice,
                BikeTypeDto.EBike => eBikePrice,
                _ => 0m
            };
        }

        return subtotal;
    }

    /// <summary>
    /// Calculates the discount amount based on discount type.
    /// </summary>
    /// <param name="subtotal">Subtotal price before discount</param>
    public decimal CalculateDiscountAmount(decimal subtotal)
    {
        if (DiscountType == DiscountTypeDto.None || DiscountAmount <= 0)
        {
            return 0m;
        }

        return DiscountType switch
        {
            DiscountTypeDto.Percentage => subtotal * (DiscountAmount / 100m),
            DiscountTypeDto.Absolute => DiscountAmount,
            _ => 0m
        };
    }

    /// <summary>
    /// Calculates the final total price after discount.
    /// </summary>
    /// <param name="subtotal">Subtotal price before discount</param>
    public decimal CalculateFinalTotal(decimal subtotal)
    {
        var discountAmount = CalculateDiscountAmount(subtotal);
        return Math.Max(0, subtotal - discountAmount);
    }
}