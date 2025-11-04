using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Web.Components.Shared;

public class BookingFormModel : IValidatableObject
{
    public int? TourId { get; set; }

    public int? CustomerId { get; set; }

    public int? CompanionId { get; set; }

    [Required(ErrorMessage = "Room type is required")]
    public RoomTypeDto RoomType { get; set; } = RoomTypeDto.SingleRoom;

    [Required(ErrorMessage = "Bike type is required for principal customer")]
    public BikeTypeDto PrincipalBikeType { get; set; } = BikeTypeDto.None;

    public BikeTypeDto? CompanionBikeType { get; set; }

    [MaxLength(ContractConstants.MaxBookingNotesLength, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }

    public BookingStatusDto Status { get; set; } = BookingStatusDto.Pending;

    public PaymentStatusDto PaymentStatus { get; set; } = PaymentStatusDto.Unpaid;

    public DiscountTypeDto DiscountType { get; set; } = DiscountTypeDto.None;

    [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be positive")]
    public decimal DiscountAmount { get; set; }

    [StringLength(ContractConstants.MaxDiscountReasonLength, MinimumLength = ContractConstants.MinDiscountReasonLength, ErrorMessage = "Discount reason must be between {2} and {1} characters")]
    public string DiscountReason { get; set; } = string.Empty;

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

    public decimal CalculateSubtotal(decimal basePrice, decimal doubleRoomSupplement, decimal regularBikePrice, decimal eBikePrice)
    {
        var roomCost = RoomType == RoomTypeDto.DoubleRoom ? doubleRoomSupplement : 0m;

        var principalBikeCost = PrincipalBikeType switch
        {
            BikeTypeDto.Regular => regularBikePrice,
            BikeTypeDto.EBike => eBikePrice,
            _ => 0m
        };

        var companionBikeCost = CompanionBikeType switch
        {
            BikeTypeDto.Regular => regularBikePrice,
            BikeTypeDto.EBike => eBikePrice,
            _ => 0m
        };

        return basePrice + roomCost + principalBikeCost + companionBikeCost;
    }

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

    public decimal CalculateFinalTotal(decimal subtotal)
    {
        var discountAmount = CalculateDiscountAmount(subtotal);
        return Math.Max(0, subtotal - discountAmount);
    }
}
