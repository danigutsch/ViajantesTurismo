using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.UnitTests.Bookings.Contracts;

public class BookingContractValidationTests
{
    private const string CompanionBikeTypeMemberName = "CompanionBikeType";

    [Fact]
    public void Discount_Validation_With_No_Discount_Should_Return_No_Errors()
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

        Assert.Empty(results);
    }

    [Fact]
    public void Discount_Validation_With_Invalid_Percentage_Discount_Should_Return_Amount_And_Reason_Errors()
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

        Assert.Equal(2, results.Length);
        Assert.Contains(results, result =>
            result.MemberNames.SequenceEqual(["DiscountAmount"]) &&
            result.ErrorMessage == $"Percentage discount cannot exceed {ContractConstants.MaxDiscountPercentage}%."
        );
        Assert.Contains(results, result =>
            result.MemberNames.SequenceEqual(["DiscountReason"]) &&
            result.ErrorMessage == $"Discount reason must be at least {ContractConstants.MinDiscountReasonLength} characters."
        );
    }

    [Fact]
    public void Booking_Validation_With_Single_Room_And_Companion_Should_Return_Companion_Error()
    {
        var result = BookingValidation.ValidateSingleRoomNoCompanion(
            RoomTypeDto.SingleOccupancy,
            Guid.CreateVersion7(),
            "CompanionCustomerId");

        Assert.NotNull(result);
        Assert.Equal(
            "Single room bookings cannot have a companion. Please select Double Room or remove the companion.",
            result.ErrorMessage);
        Assert.Equal(["CompanionCustomerId"], result.MemberNames);
    }

    [Theory]
    [MemberData(nameof(AllowedRoomAndCompanionCombinations))]
    public void Booking_Validation_With_Allowed_Room_And_Companion_Combination_Should_Return_No_Error(
        RoomTypeDto roomType,
        Guid? companionCustomerId)
    {
        var result = BookingValidation.ValidateSingleRoomNoCompanion(
            roomType,
            companionCustomerId,
            "CompanionCustomerId");

        Assert.Null(result);
    }

    [Fact]
    public void Booking_Validation_With_Companion_And_Missing_Bike_Type_Should_Return_Companion_Bike_Type_Error()
    {
        var result = BookingValidation.ValidateCompanionHasBikeType(
            Guid.CreateVersion7(),
            null,
            CompanionBikeTypeMemberName);

        Assert.NotNull(result);
        Assert.Equal(
            "Companion bike type is required when a companion is selected.",
            result.ErrorMessage);
        Assert.Equal([CompanionBikeTypeMemberName], result.MemberNames);
    }

    [Theory]
    [MemberData(nameof(ValidCompanionBikeTypeStates))]
    public void Booking_Validation_With_Valid_Companion_Bike_Type_State_Should_Return_No_Error(
        Guid? companionCustomerId,
        BikeTypeDto? companionBikeType)
    {
        var result = BookingValidation.ValidateCompanionHasBikeType(
            companionCustomerId,
            companionBikeType,
            CompanionBikeTypeMemberName);

        Assert.Null(result);
    }

    [Fact]
    public void Booking_Validation_With_Principal_Bike_Type_None_Should_Return_Principal_Bike_Type_Error()
    {
        var result = BookingValidation.ValidatePrincipalBikeType(BikeTypeDto.None, "PrincipalBikeType");

        Assert.NotNull(result);
        Assert.Equal(
            "Principal customer must select a bike type (Regular or E-Bike).",
            result.ErrorMessage);
        Assert.Equal(["PrincipalBikeType"], result.MemberNames);
    }

    [Theory]
    [InlineData(BikeTypeDto.Regular)]
    [InlineData(BikeTypeDto.EBike)]
    public void Booking_Validation_With_Valid_Principal_Bike_Type_Should_Return_No_Error(BikeTypeDto principalBikeType)
    {
        var result = BookingValidation.ValidatePrincipalBikeType(principalBikeType, "PrincipalBikeType");

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(BikeTypeDto.Regular)]
    [InlineData(BikeTypeDto.EBike)]
    public void Booking_Validation_With_Allowed_Companion_Bike_Type_Value_Should_Return_No_Error(BikeTypeDto? companionBikeType)
    {
        var result = BookingValidation.ValidateCompanionBikeTypeNotNone(companionBikeType, CompanionBikeTypeMemberName);

        Assert.Null(result);
    }

    [Fact]
    public void Create_Booking_Dto_Validate_With_Invalid_Discount_Should_Return_Discount_Validation_Errors()
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

        Assert.Equal(2, results.Length);
        Assert.Contains(results, result =>
            result.MemberNames.SequenceEqual([nameof(CreateBookingDto.DiscountAmount)]) &&
            result.ErrorMessage == "Discount amount must be greater than 0 when a discount is applied.");
        Assert.Contains(results, result =>
            result.MemberNames.SequenceEqual([nameof(CreateBookingDto.DiscountReason)]) &&
            result.ErrorMessage == "Discount reason is required when applying a discount.");
    }

    [Fact]
    public void Update_Booking_Details_Dto_Validate_With_Invalid_Combination_Should_Return_All_Booking_Rule_Errors()
    {
        var dto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.SingleOccupancy,
            PrincipalBikeType = BikeTypeDto.None,
            CompanionCustomerId = Guid.CreateVersion7(),
            CompanionBikeType = BikeTypeDto.None
        };

        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        Assert.Equal(3, results.Length);
        Assert.Contains(results, result =>
            result.MemberNames.SequenceEqual([nameof(UpdateBookingDetailsDto.CompanionCustomerId)]) &&
            result.ErrorMessage == "Single room bookings cannot have a companion. Please select Double Room or remove the companion.");
        Assert.Contains(results, result =>
            result.MemberNames.SequenceEqual([nameof(UpdateBookingDetailsDto.PrincipalBikeType)]) &&
            result.ErrorMessage == "Principal customer must select a bike type (Regular or E-Bike).");
        Assert.Contains(results, result =>
            result.MemberNames.SequenceEqual([nameof(UpdateBookingDetailsDto.CompanionBikeType)]) &&
            result.ErrorMessage == "Companion must select a bike type (Regular or E-Bike).");
    }

    [Fact]
    public void Update_Booking_Discount_Dto_Validate_With_Invalid_Reason_Should_Return_Discount_Reason_Error()
    {
        var dto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 25m,
            DiscountReason = "short"
        };

        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        Assert.Single(results);
        Assert.Equal([nameof(UpdateBookingDiscountDto.DiscountReason)], results[0].MemberNames);
        Assert.Equal(
            $"Discount reason must be at least {ContractConstants.MinDiscountReasonLength} characters.",
            results[0].ErrorMessage);
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
