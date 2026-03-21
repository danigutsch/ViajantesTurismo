using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.UnitTests.Contracts;

public class ContractValidationTests
{
    private const string CompanionBikeTypeMemberName = "CompanionBikeType";

    [Fact]
    public void Discount_Validation_With_No_Discount_Should_Return_No_Errors()
    {
        // Arrange
        var results = DiscountValidation.Validate(
                DiscountTypeDto.None,
                0m,
                null,
                ContractConstants.MaxDiscountPercentage,
                ContractConstants.MinDiscountReasonLength,
                "DiscountAmount",
                "DiscountReason")
            .ToArray();

        // Act
        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Discount_Validation_With_Invalid_Percentage_Discount_Should_Return_Amount_And_Reason_Errors()
    {
        // Arrange
        var results = DiscountValidation.Validate(
                DiscountTypeDto.Percentage,
                ContractConstants.MaxDiscountPercentage + 1,
                "short",
                ContractConstants.MaxDiscountPercentage,
                ContractConstants.MinDiscountReasonLength,
                "DiscountAmount",
                "DiscountReason")
            .ToArray();

        // Act
        // Assert
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
        // Arrange
        var result = BookingValidation.ValidateSingleRoomNoCompanion(
            RoomTypeDto.SingleOccupancy,
            Guid.CreateVersion7(),
            "CompanionCustomerId");

        // Act
        // Assert
        Assert.NotNull(result);
        Assert.Equal(
            "Single room bookings cannot have a companion. Please select Double Room or remove the companion.",
            result.ErrorMessage);
        Assert.Equal(["CompanionCustomerId"], result.MemberNames);
    }

    [Theory]
    [InlineData(RoomTypeDto.SingleOccupancy, false)]
    [InlineData(RoomTypeDto.DoubleOccupancy, true)]
    public void Booking_Validation_With_Allowed_Room_And_Companion_Combination_Should_Return_No_Error(
        RoomTypeDto roomType,
        bool hasCompanion)
    {
        // Arrange
        Guid? companionCustomerId = hasCompanion ? Guid.CreateVersion7() : null;

        // Act
        var result = BookingValidation.ValidateSingleRoomNoCompanion(
            roomType,
            companionCustomerId,
            "CompanionCustomerId");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Booking_Validation_With_Companion_And_Missing_Bike_Type_Should_Return_Companion_Bike_Type_Error()
    {
        // Arrange
        var result = BookingValidation.ValidateCompanionHasBikeType(
            Guid.CreateVersion7(),
            null,
            CompanionBikeTypeMemberName);

        // Act
        // Assert
        Assert.NotNull(result);
        Assert.Equal(
            "Companion bike type is required when a companion is selected.",
            result.ErrorMessage);
        Assert.Equal([CompanionBikeTypeMemberName], result.MemberNames);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Booking_Validation_With_Valid_Companion_Bike_Type_State_Should_Return_No_Error(bool hasCompanion)
    {
        // Arrange
        Guid? companionCustomerId = hasCompanion ? Guid.CreateVersion7() : null;
        BikeTypeDto? companionBikeType = hasCompanion ? BikeTypeDto.Regular : null;

        // Act
        var result = BookingValidation.ValidateCompanionHasBikeType(
            companionCustomerId,
            companionBikeType,
            CompanionBikeTypeMemberName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Booking_Validation_With_Principal_Bike_Type_None_Should_Return_Principal_Bike_Type_Error()
    {
        // Arrange
        var result = BookingValidation.ValidatePrincipalBikeType(BikeTypeDto.None, "PrincipalBikeType");

        // Act
        // Assert
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
        // Arrange
        // Act
        var result = BookingValidation.ValidatePrincipalBikeType(principalBikeType, "PrincipalBikeType");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(BikeTypeDto.Regular)]
    [InlineData(BikeTypeDto.EBike)]
    public void Booking_Validation_With_Allowed_Companion_Bike_Type_Value_Should_Return_No_Error(BikeTypeDto? companionBikeType)
    {
        // Arrange
        // Act
        var result = BookingValidation.ValidateCompanionBikeTypeNotNone(companionBikeType, CompanionBikeTypeMemberName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Tour_Validation_With_Minimum_Duration_Should_Return_Duration_Error()
    {
        // Arrange
        var startDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddDays(ContractConstants.MinimumTourDurationDays);

        // Act
        var result = TourValidation.ValidateDuration(
            startDate,
            endDate,
            ContractConstants.MinimumTourDurationDays,
            "StartDate",
            "EndDate");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(
            $"The tour must be at least {ContractConstants.MinimumTourDurationDays} days long. End date must be more than {ContractConstants.MinimumTourDurationDays} days after start date.",
            result.ErrorMessage);
        Assert.Equal(["StartDate", "EndDate"], result.MemberNames);
    }

    [Fact]
    public void Create_Booking_Dto_Validate_With_Invalid_Discount_Should_Return_Discount_Validation_Errors()
    {
        // Arrange
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

        // Act
        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        // Assert
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
        // Arrange
        var dto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.SingleOccupancy,
            PrincipalBikeType = BikeTypeDto.None,
            CompanionCustomerId = Guid.CreateVersion7(),
            CompanionBikeType = BikeTypeDto.None
        };

        // Act
        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        // Assert
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
        // Arrange
        var dto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 25m,
            DiscountReason = "short"
        };

        // Act
        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        // Assert
        Assert.Single(results);
        Assert.Equal([nameof(UpdateBookingDiscountDto.DiscountReason)], results[0].MemberNames);
        Assert.Equal(
            $"Discount reason must be at least {ContractConstants.MinDiscountReasonLength} characters.",
            results[0].ErrorMessage);
    }

    [Fact]
    public void Create_Tour_Dto_Validate_With_Too_Short_Duration_Should_Return_Duration_Error()
    {
        // Arrange
        var startDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dto = new CreateTourDto
        {
            Identifier = "TOUR-001",
            Name = "Contract Tour",
            StartDate = startDate,
            EndDate = startDate.AddDays(ContractConstants.MinimumTourDurationDays),
            Price = 1000m,
            SingleRoomSupplementPrice = 250m,
            RegularBikePrice = 50m,
            EBikePrice = 100m,
            Currency = CurrencyDto.Euro,
            IncludedServices = ["Hotel"],
            MinCustomers = ContractConstants.MinTourCustomers,
            MaxCustomers = ContractConstants.MaxTourCustomers
        };

        // Act
        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        // Assert
        Assert.Single(results);
        Assert.Equal([nameof(CreateTourDto.StartDate), nameof(CreateTourDto.EndDate)], results[0].MemberNames);
    }

    [Fact]
    public void Update_Tour_Dto_Validate_With_Valid_Duration_Should_Return_No_Errors()
    {
        // Arrange
        var startDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dto = new UpdateTourDto
        {
            Identifier = "TOUR-002",
            Name = "Updated Contract Tour",
            StartDate = startDate,
            EndDate = startDate.AddDays(ContractConstants.MinimumTourDurationDays + 1),
            Price = 1100m,
            SingleRoomSupplementPrice = 250m,
            RegularBikePrice = 50m,
            EBikePrice = 100m,
            Currency = CurrencyDto.UsDollar,
            IncludedServices = ["Hotel", "Breakfast"],
            MinCustomers = ContractConstants.MinTourCustomers,
            MaxCustomers = ContractConstants.MaxTourCustomers
        };

        // Act
        var results = dto.Validate(new ValidationContext(dto)).ToArray();

        // Assert
        Assert.Empty(results);
    }
}
