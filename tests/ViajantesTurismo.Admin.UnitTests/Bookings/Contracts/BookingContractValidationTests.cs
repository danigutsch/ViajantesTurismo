using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.Bookings.Contracts;

public class BookingContractValidationTests
{
    private const string CompanionBikeTypeMemberName = "CompanionBikeType";

    [Fact]
    public void Discount_validation_with_no_discount_should_return_no_errors()
    {
        var results = DiscountValidation.Validate(
                DiscountTypeDto.None,
                0m,
                null,
                ContractConstants.MaxDiscountPercentage,
                ContractConstants.MinDiscountReasonLength,
                "DiscountAmount",
                "DiscountReason")
            .ToArray();

        (results).ShouldBeEmpty();
    }

    [Fact]
    public void Discount_validation_with_invalid_percentage_discount_should_return_amount_and_reason_errors()
    {
        var results = DiscountValidation.Validate(
                DiscountTypeDto.Percentage,
                ContractConstants.MaxDiscountPercentage + 1,
                "short",
                ContractConstants.MaxDiscountPercentage,
                ContractConstants.MinDiscountReasonLength,
                "DiscountAmount",
                "DiscountReason")
            .ToArray();

        (results.Length).ShouldBe(2);
        (results).ShouldContain(result =>
            result.MemberNames.SequenceEqual(["DiscountAmount"]) &&
            result.ErrorMessage == $"Percentage discount cannot exceed {ContractConstants.MaxDiscountPercentage}%.");
        (results).ShouldContain(result =>
            result.MemberNames.SequenceEqual(["DiscountReason"]) &&
            result.ErrorMessage == $"Discount reason must be at least {ContractConstants.MinDiscountReasonLength} characters.");
    }

    [Fact]
    public void Booking_validation_with_single_room_and_companion_should_return_companion_error()
    {
        var result = BookingValidation.ValidateSingleRoomNoCompanion(
            RoomTypeDto.SingleOccupancy,
            Guid.CreateVersion7(),
            "CompanionCustomerId");

        _ = (result).ShouldNotBeNull();
        (result.ErrorMessage).ShouldBe("Single room bookings cannot have a companion. Please select Double Room or remove the companion.");
        (result.MemberNames).ShouldBe(["CompanionCustomerId"]);
    }

    [Theory]
    [MemberData(nameof(AllowedRoomAndCompanionCombinations))]
    public void Booking_validation_with_allowed_room_and_companion_combination_should_return_no_error(
        RoomTypeDto roomType,
        Guid? companionCustomerId)
    {
        var result = BookingValidation.ValidateSingleRoomNoCompanion(
            roomType,
            companionCustomerId,
            "CompanionCustomerId");

        (result).ShouldBeNull();
    }

    [Fact]
    public void Booking_validation_with_companion_and_missing_bike_type_should_return_companion_bike_type_error()
    {
        var result = BookingValidation.ValidateCompanionHasBikeType(
            Guid.CreateVersion7(),
            null,
            CompanionBikeTypeMemberName);

        _ = (result).ShouldNotBeNull();
        (result.ErrorMessage).ShouldBe("Companion bike type is required when a companion is selected.");
        (result.MemberNames).ShouldBe([CompanionBikeTypeMemberName]);
    }

    [Theory]
    [MemberData(nameof(ValidCompanionBikeTypeStates))]
    public void Booking_validation_with_valid_companion_bike_type_state_should_return_no_error(
        Guid? companionCustomerId,
        BikeTypeDto? companionBikeType)
    {
        var result = BookingValidation.ValidateCompanionHasBikeType(
            companionCustomerId,
            companionBikeType,
            CompanionBikeTypeMemberName);

        (result).ShouldBeNull();
    }

    [Fact]
    public void Booking_validation_with_principal_bike_type_none_should_return_principal_bike_type_error()
    {
        var result = BookingValidation.ValidatePrincipalBikeType(BikeTypeDto.None, "PrincipalBikeType");

        _ = (result).ShouldNotBeNull();
        (result.ErrorMessage).ShouldBe("Principal customer must select a bike type (Regular or E-Bike).");
        (result.MemberNames).ShouldBe(["PrincipalBikeType"]);
    }

    [Theory]
    [InlineData(BikeTypeDto.Regular)]
    [InlineData(BikeTypeDto.EBike)]
    public void Booking_validation_with_valid_principal_bike_type_should_return_no_error(BikeTypeDto principalBikeType)
    {
        var result = BookingValidation.ValidatePrincipalBikeType(principalBikeType, "PrincipalBikeType");

        (result).ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(BikeTypeDto.Regular)]
    [InlineData(BikeTypeDto.EBike)]
    public void Booking_validation_with_allowed_companion_bike_type_value_should_return_no_error(BikeTypeDto? companionBikeType)
    {
        var result = BookingValidation.ValidateCompanionBikeTypeNotNone(companionBikeType, CompanionBikeTypeMemberName);

        (result).ShouldBeNull();
    }

    [Fact]
    public void Create_booking_dto_validate_with_invalid_discount_should_return_discount_validation_errors()
    {
        var dto = new CreateBookingDto
        {
            TourId = Guid.CreateVersion7(),
            PrincipalCustomerId = Guid.CreateVersion7(),
            PrincipalBikeType = BikeTypeDto.Regular,
            RoomType = RoomTypeDto.SingleOccupancy,
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 0,
            DiscountReason = null
        };

        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        (results.Length).ShouldBe(2);
        (results).ShouldContain(result =>
            result.MemberNames.SequenceEqual([nameof(CreateBookingDto.DiscountAmount)]) &&
            result.ErrorMessage == "Discount amount must be greater than 0 when a discount is applied.");
        (results).ShouldContain(result =>
            result.MemberNames.SequenceEqual([nameof(CreateBookingDto.DiscountReason)]) &&
            result.ErrorMessage == "Discount reason is required when applying a discount.");
    }

    [Fact]
    public void Update_booking_details_dto_validate_with_invalid_combination_should_return_all_booking_rule_errors()
    {
        var dto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.SingleOccupancy,
            PrincipalBikeType = BikeTypeDto.None,
            CompanionCustomerId = Guid.CreateVersion7(),
            CompanionBikeType = BikeTypeDto.None
        };

        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        (results.Length).ShouldBe(3);
        (results).ShouldContain(result =>
            result.MemberNames.SequenceEqual([nameof(UpdateBookingDetailsDto.CompanionCustomerId)]) &&
            result.ErrorMessage == "Single room bookings cannot have a companion. Please select Double Room or remove the companion.");
        (results).ShouldContain(result =>
            result.MemberNames.SequenceEqual([nameof(UpdateBookingDetailsDto.PrincipalBikeType)]) &&
            result.ErrorMessage == "Principal customer must select a bike type (Regular or E-Bike).");
        (results).ShouldContain(result =>
            result.MemberNames.SequenceEqual([nameof(UpdateBookingDetailsDto.CompanionBikeType)]) &&
            result.ErrorMessage == "Companion must select a bike type (Regular or E-Bike).");
    }

    [Fact]
    public void Update_booking_discount_dto_validate_with_invalid_reason_should_return_discount_reason_error()
    {
        var dto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 25m,
            DiscountReason = "short"
        };

        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        (results).ShouldHaveSingleItem();
        (results[0].MemberNames).ShouldBe([nameof(UpdateBookingDiscountDto.DiscountReason)]);
        (results[0].ErrorMessage).ShouldBe($"Discount reason must be at least {ContractConstants.MinDiscountReasonLength} characters.");
    }

    public static TheoryData<RoomTypeDto, Guid?> AllowedRoomAndCompanionCombinations =>
        new()
        {
            { RoomTypeDto.SingleOccupancy, null },
            { RoomTypeDto.DoubleOccupancy, Guid.CreateVersion7() },
        };

    public static TheoryData<Guid?, BikeTypeDto?> ValidCompanionBikeTypeStates =>
        new()
        {
            { Guid.CreateVersion7(), BikeTypeDto.Regular },
            { null, null },
        };
}
