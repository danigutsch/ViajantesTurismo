using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Shared;

/// <summary>
/// Form model for creating or editing bookings. Provides mutable properties for Blazor binding
/// and converts to an immutable <see cref="CreateBookingDto"/> for creation requests.
/// </summary>
public class BookingFormModel : IValidatableObject
{
    public Guid? TourId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? CompanionId { get; set; }

    [Required(ErrorMessage = "Room type is required")]
    public RoomTypeDto RoomType { get; set; } = RoomTypeDto.DoubleOccupancy;

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
    /// Validates booking and discount fields using the existing booking contract rules.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TourId.HasValue)
        {
            yield return new ValidationResult("Tour is required", [nameof(TourId)]);
        }

        if (!CustomerId.HasValue)
        {
            yield return new ValidationResult("Customer is required", [nameof(CustomerId)]);
        }

        var singleRoomResult = BookingValidation.ValidateSingleRoomNoCompanion(
            RoomType,
            CompanionId,
            nameof(CompanionId));
        if (singleRoomResult is not null)
        {
            yield return singleRoomResult;
        }

        var companionBikeResult = BookingValidation.ValidateCompanionHasBikeType(
            CompanionId,
            CompanionBikeType,
            nameof(CompanionBikeType));
        if (companionBikeResult is not null)
        {
            yield return companionBikeResult;
        }

        var principalBikeResult = BookingValidation.ValidatePrincipalBikeType(
            PrincipalBikeType,
            nameof(PrincipalBikeType));
        if (principalBikeResult is not null)
        {
            yield return principalBikeResult;
        }

        var companionBikeNoneResult = BookingValidation.ValidateCompanionBikeTypeNotNone(
            CompanionBikeType,
            nameof(CompanionBikeType));
        if (companionBikeNoneResult is not null)
        {
            yield return companionBikeNoneResult;
        }

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
