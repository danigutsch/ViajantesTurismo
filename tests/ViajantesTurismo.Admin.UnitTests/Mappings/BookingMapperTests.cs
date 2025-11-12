using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public class BookingMapperTests
{
    [Theory]
    [InlineData(BikeTypeDto.None, BikeType.None)]
    [InlineData(BikeTypeDto.Regular, BikeType.Regular)]
    [InlineData(BikeTypeDto.EBike, BikeType.EBike)]
    public void Map_To_Bike_Type_Should_Map_All_Valid_Values(BikeTypeDto dto, BikeType expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBikeType(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Bike_Type_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<BikeTypeDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBikeType(dtoValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Theory]
    [InlineData(BikeType.None, BikeTypeDto.None)]
    [InlineData(BikeType.Regular, BikeTypeDto.Regular)]
    [InlineData(BikeType.EBike, BikeTypeDto.EBike)]
    public void Map_To_Bike_Type_Dto_Should_Map_All_Valid_Values(BikeType domain, BikeTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBikeTypeDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Bike_Type_Dto_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<BikeType>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBikeTypeDto(domainValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Theory]
    [InlineData(RoomTypeDto.SingleRoom, RoomType.SingleRoom)]
    [InlineData(RoomTypeDto.DoubleRoom, RoomType.DoubleRoom)]
    public void Map_To_Room_Type_Should_Map_All_Valid_Values(RoomTypeDto dto, RoomType expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToRoomType(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Room_Type_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<RoomTypeDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToRoomType(dtoValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Theory]
    [InlineData(BookingStatusDto.Pending, BookingStatus.Pending)]
    [InlineData(BookingStatusDto.Confirmed, BookingStatus.Confirmed)]
    [InlineData(BookingStatusDto.Cancelled, BookingStatus.Cancelled)]
    [InlineData(BookingStatusDto.Completed, BookingStatus.Completed)]
    public void Map_To_Booking_Status_Should_Map_All_Valid_Values(BookingStatusDto dto, BookingStatus expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBookingStatus(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Booking_Status_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<BookingStatusDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBookingStatus(dtoValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Theory]
    [InlineData(PaymentStatusDto.Unpaid, PaymentStatus.Unpaid)]
    [InlineData(PaymentStatusDto.PartiallyPaid, PaymentStatus.PartiallyPaid)]
    [InlineData(PaymentStatusDto.Paid, PaymentStatus.Paid)]
    [InlineData(PaymentStatusDto.Refunded, PaymentStatus.Refunded)]
    public void Map_To_Payment_Status_Should_Map_All_Valid_Values(PaymentStatusDto dto, PaymentStatus expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToPaymentStatus(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Payment_Status_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<PaymentStatusDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToPaymentStatus(dtoValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Theory]
    [InlineData(DiscountTypeDto.None, DiscountType.None)]
    [InlineData(DiscountTypeDto.Percentage, DiscountType.Percentage)]
    [InlineData(DiscountTypeDto.Absolute, DiscountType.Absolute)]
    public void Map_To_Discount_Type_Should_Map_All_Valid_Values(DiscountTypeDto dto, DiscountType expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToDiscountType(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Discount_Type_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<DiscountTypeDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToDiscountType(dtoValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Theory]
    [InlineData(DiscountType.None, DiscountTypeDto.None)]
    [InlineData(DiscountType.Percentage, DiscountTypeDto.Percentage)]
    [InlineData(DiscountType.Absolute, DiscountTypeDto.Absolute)]
    public void Map_To_Discount_Type_Dto_Should_Map_All_Valid_Values(DiscountType domain, DiscountTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToDiscountTypeDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Discount_Type_Dto_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<DiscountType>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToDiscountTypeDto(domainValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void Map_To_Bike_Type_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const BikeTypeDto invalidValue = (BikeTypeDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToBikeType(invalidValue));
        Assert.Contains("Invalid bike type value", exception.Message);
    }

    [Fact]
    public void Map_To_Bike_Type_Dto_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const BikeType invalidValue = (BikeType)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToBikeTypeDto(invalidValue));
        Assert.Contains("Invalid bike type value", exception.Message);
    }

    [Fact]
    public void Map_To_Room_Type_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const RoomTypeDto invalidValue = (RoomTypeDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToRoomType(invalidValue));
        Assert.Contains("Invalid room type value", exception.Message);
    }

    [Fact]
    public void Map_To_Booking_Status_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const BookingStatusDto invalidValue = (BookingStatusDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToBookingStatus(invalidValue));
        Assert.Contains("Invalid booking status value", exception.Message);
    }

    [Fact]
    public void Map_To_Payment_Status_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const PaymentStatusDto invalidValue = (PaymentStatusDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToPaymentStatus(invalidValue));
        Assert.Contains("Invalid payment status value", exception.Message);
    }

    [Fact]
    public void Map_To_Discount_Type_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const DiscountTypeDto invalidValue = (DiscountTypeDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToDiscountType(invalidValue));
        Assert.Contains("Invalid discount type value", exception.Message);
    }

    [Fact]
    public void Map_To_Discount_Type_Dto_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const DiscountType invalidValue = (DiscountType)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToDiscountTypeDto(invalidValue));
        Assert.Contains("Invalid discount type value", exception.Message);
    }

    [Theory]
    [InlineData(PaymentMethodDto.CreditCard, PaymentMethod.CreditCard)]
    [InlineData(PaymentMethodDto.BankTransfer, PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethodDto.Cash, PaymentMethod.Cash)]
    [InlineData(PaymentMethodDto.Check, PaymentMethod.Check)]
    [InlineData(PaymentMethodDto.PayPal, PaymentMethod.PayPal)]
    [InlineData(PaymentMethodDto.Other, PaymentMethod.Other)]
    public void Map_To_Payment_Method_Should_Map_All_Valid_Values(PaymentMethodDto dto, PaymentMethod expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToPaymentMethod(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Payment_Method_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<PaymentMethodDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToPaymentMethod(dtoValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void Map_To_Payment_Method_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const PaymentMethodDto invalidValue = (PaymentMethodDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToPaymentMethod(invalidValue));
        Assert.Contains("Invalid payment method value", exception.Message);
    }

    [Theory]
    [InlineData(PaymentMethod.CreditCard, PaymentMethodDto.CreditCard)]
    [InlineData(PaymentMethod.BankTransfer, PaymentMethodDto.BankTransfer)]
    [InlineData(PaymentMethod.Cash, PaymentMethodDto.Cash)]
    [InlineData(PaymentMethod.Check, PaymentMethodDto.Check)]
    [InlineData(PaymentMethod.PayPal, PaymentMethodDto.PayPal)]
    [InlineData(PaymentMethod.Other, PaymentMethodDto.Other)]
    public void Map_To_Payment_Method_Dto_Should_Map_All_Valid_Values(PaymentMethod domain, PaymentMethodDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToPaymentMethodDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Payment_Method_Dto_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<PaymentMethod>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToPaymentMethodDto(domainValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void Map_To_Payment_Method_Dto_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const PaymentMethod invalidValue = (PaymentMethod)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToPaymentMethodDto(invalidValue));
        Assert.Contains("Invalid payment method value", exception.Message);
    }

    [Fact]
    public void Map_To_Payment_Dto_Should_Map_All_Properties()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var paymentDate = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var timeProvider = TimeProvider.System;

        var paymentResult = Payment.Create(
            bookingId,
            150.50m,
            paymentDate,
            PaymentMethod.CreditCard,
            timeProvider,
            "REF-12345",
            "Payment for tour booking"
        );

        Assert.True(paymentResult.IsSuccess);
        var payment = paymentResult.Value;

        // Act
        var result = BookingMapper.MapToPaymentDto(payment);

        // Assert
        Assert.Equal(payment.Id, result.Id);
        Assert.Equal(bookingId, result.BookingId);
        Assert.Equal(150.50m, result.Amount);
        Assert.Equal(paymentDate, result.PaymentDate);
        Assert.Equal(PaymentMethodDto.CreditCard, result.Method);
        Assert.Equal("REF-12345", result.ReferenceNumber);
        Assert.Equal("Payment for tour booking", result.Notes);
        Assert.Equal(payment.RecordedAt, result.RecordedAt);
    }

    [Fact]
    public void Map_To_Payment_Dto_With_Null_Payment_Should_Throw_Argument_Null_Exception()
    {
        // Arrange
        Payment? payment = null;

        // Act
        // Assert
        Assert.Throws<ArgumentNullException>(() => BookingMapper.MapToPaymentDto(payment!));
    }

    [Fact]
    public void Map_To_Payment_Dto_With_Null_Optional_Fields_Should_Map_Correctly()
    {
        // Arrange
        var bookingId = Guid.CreateVersion7();
        var paymentDate = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var timeProvider = TimeProvider.System;

        var paymentResult = Payment.Create(
            bookingId,
            100.00m,
            paymentDate,
            PaymentMethod.Cash,
            timeProvider,
            null,
            null
        );

        Assert.True(paymentResult.IsSuccess);
        var payment = paymentResult.Value;

        // Act
        var result = BookingMapper.MapToPaymentDto(payment);

        // Assert
        Assert.Equal(payment.Id, result.Id);
        Assert.Equal(bookingId, result.BookingId);
        Assert.Equal(100.00m, result.Amount);
        Assert.Equal(paymentDate, result.PaymentDate);
        Assert.Equal(PaymentMethodDto.Cash, result.Method);
        Assert.Null(result.ReferenceNumber);
        Assert.Null(result.Notes);
        Assert.Equal(payment.RecordedAt, result.RecordedAt);
    }

    [Theory]
    [InlineData(PaymentMethod.CreditCard, PaymentMethodDto.CreditCard)]
    [InlineData(PaymentMethod.BankTransfer, PaymentMethodDto.BankTransfer)]
    [InlineData(PaymentMethod.Cash, PaymentMethodDto.Cash)]
    [InlineData(PaymentMethod.Check, PaymentMethodDto.Check)]
    [InlineData(PaymentMethod.PayPal, PaymentMethodDto.PayPal)]
    [InlineData(PaymentMethod.Other, PaymentMethodDto.Other)]
    public void Map_To_Payment_Dto_Should_Map_All_Payment_Methods(PaymentMethod domainMethod, PaymentMethodDto expectedDto)
    {
        // Arrange
        var paymentDate = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var timeProvider = TimeProvider.System;

        var paymentResult = Payment.Create(
            Guid.CreateVersion7(),
            100.00m,
            paymentDate,
            domainMethod,
            timeProvider,
            null,
            null
        );

        Assert.True(paymentResult.IsSuccess);
        var payment = paymentResult.Value;

        // Act
        var result = BookingMapper.MapToPaymentDto(payment);

        // Assert
        Assert.Equal(expectedDto, result.Method);
    }

    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatusDto.Pending)]
    [InlineData(BookingStatus.Confirmed, BookingStatusDto.Confirmed)]
    [InlineData(BookingStatus.Cancelled, BookingStatusDto.Cancelled)]
    [InlineData(BookingStatus.Completed, BookingStatusDto.Completed)]
    public void Map_To_Booking_Status_Dto_Should_Map_All_Valid_Values(BookingStatus domain, BookingStatusDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBookingStatusDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Booking_Status_Dto_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<BookingStatus>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBookingStatusDto(domainValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void Map_To_Booking_Status_Dto_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const BookingStatus invalidValue = (BookingStatus)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToBookingStatusDto(invalidValue));
        Assert.Contains("Invalid booking status value", exception.Message);
    }

    [Theory]
    [InlineData(PaymentStatus.Unpaid, PaymentStatusDto.Unpaid)]
    [InlineData(PaymentStatus.PartiallyPaid, PaymentStatusDto.PartiallyPaid)]
    [InlineData(PaymentStatus.Paid, PaymentStatusDto.Paid)]
    [InlineData(PaymentStatus.Refunded, PaymentStatusDto.Refunded)]
    public void Map_To_Payment_Status_Dto_Should_Map_All_Valid_Values(PaymentStatus domain, PaymentStatusDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToPaymentStatusDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Payment_Status_Dto_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<PaymentStatus>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToPaymentStatusDto(domainValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void Map_To_Payment_Status_Dto_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const PaymentStatus invalidValue = (PaymentStatus)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToPaymentStatusDto(invalidValue));
        Assert.Contains("Invalid payment status value", exception.Message);
    }

    [Theory]
    [InlineData(RoomType.SingleRoom, RoomTypeDto.SingleRoom)]
    [InlineData(RoomType.DoubleRoom, RoomTypeDto.DoubleRoom)]
    public void Map_To_Room_Type_Dto_Should_Map_All_Valid_Values(RoomType domain, RoomTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToRoomTypeDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Room_Type_Dto_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<RoomType>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToRoomTypeDto(domainValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void Map_To_Room_Type_Dto_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const RoomType invalidValue = (RoomType)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToRoomTypeDto(invalidValue));
        Assert.Contains("Invalid room type value", exception.Message);
    }

    [Theory]
    [InlineData(BedType.SingleBed, BedTypeDto.SingleBed)]
    [InlineData(BedType.DoubleBed, BedTypeDto.DoubleBed)]
    public void Map_To_Bed_Type_Dto_Should_Map_All_Valid_Values(BedType domain, BedTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBedTypeDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Map_To_Bed_Type_Dto_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<BedType>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBedTypeDto(domainValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void Map_To_Bed_Type_Dto_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const BedType invalidValue = (BedType)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToBedTypeDto(invalidValue));
        Assert.Contains("Invalid bed type value", exception.Message);
    }
}
