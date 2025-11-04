using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public class BookingMapperTests
{
    [Theory]
    [InlineData(BikeTypeDto.None, BikeType.None)]
    [InlineData(BikeTypeDto.Regular, BikeType.Regular)]
    [InlineData(BikeTypeDto.EBike, BikeType.EBike)]
    public void MapToBikeType_ShouldMapAllValidValues(BikeTypeDto dto, BikeType expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBikeType(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToBikeType_ShouldCoverAllEnumValues()
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
    public void MapToBikeTypeDto_ShouldMapAllValidValues(BikeType domain, BikeTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBikeTypeDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToBikeTypeDto_ShouldCoverAllEnumValues()
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
    public void MapToRoomType_ShouldMapAllValidValues(RoomTypeDto dto, RoomType expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToRoomType(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToRoomType_ShouldCoverAllEnumValues()
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
    public void MapToBookingStatus_ShouldMapAllValidValues(BookingStatusDto dto, BookingStatus expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBookingStatus(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToBookingStatus_ShouldCoverAllEnumValues()
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
    public void MapToPaymentStatus_ShouldMapAllValidValues(PaymentStatusDto dto, PaymentStatus expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToPaymentStatus(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToPaymentStatus_ShouldCoverAllEnumValues()
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
    public void MapToDiscountType_ShouldMapAllValidValues(DiscountTypeDto dto, DiscountType expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToDiscountType(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToDiscountType_ShouldCoverAllEnumValues()
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
    public void MapToDiscountTypeDto_ShouldMapAllValidValues(DiscountType domain, DiscountTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToDiscountTypeDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToDiscountTypeDto_ShouldCoverAllEnumValues()
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
    public void MapToBikeType_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const BikeTypeDto invalidValue = (BikeTypeDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToBikeType(invalidValue));
        Assert.Contains("Invalid bike type value", exception.Message);
    }

    [Fact]
    public void MapToBikeTypeDto_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const BikeType invalidValue = (BikeType)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToBikeTypeDto(invalidValue));
        Assert.Contains("Invalid bike type value", exception.Message);
    }

    [Fact]
    public void MapToRoomType_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const RoomTypeDto invalidValue = (RoomTypeDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToRoomType(invalidValue));
        Assert.Contains("Invalid room type value", exception.Message);
    }

    [Fact]
    public void MapToBookingStatus_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const BookingStatusDto invalidValue = (BookingStatusDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToBookingStatus(invalidValue));
        Assert.Contains("Invalid booking status value", exception.Message);
    }

    [Fact]
    public void MapToPaymentStatus_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const PaymentStatusDto invalidValue = (PaymentStatusDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToPaymentStatus(invalidValue));
        Assert.Contains("Invalid payment status value", exception.Message);
    }

    [Fact]
    public void MapToDiscountType_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const DiscountTypeDto invalidValue = (DiscountTypeDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToDiscountType(invalidValue));
        Assert.Contains("Invalid discount type value", exception.Message);
    }

    [Fact]
    public void MapToDiscountTypeDto_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
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
    public void MapToPaymentMethod_ShouldMapAllValidValues(PaymentMethodDto dto, PaymentMethod expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToPaymentMethod(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToPaymentMethod_ShouldCoverAllEnumValues()
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
    public void MapToPaymentMethod_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
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
    public void MapToPaymentMethodDto_ShouldMapAllValidValues(PaymentMethod domain, PaymentMethodDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToPaymentMethodDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToPaymentMethodDto_ShouldCoverAllEnumValues()
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
    public void MapToPaymentMethodDto_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const PaymentMethod invalidValue = (PaymentMethod)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToPaymentMethodDto(invalidValue));
        Assert.Contains("Invalid payment method value", exception.Message);
    }

    [Fact]
    public void MapToPaymentDto_ShouldMapAllProperties()
    {
        // Arrange
        var bookingId = 1L;
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
    public void MapToPaymentDto_WithNullPayment_ShouldThrowArgumentNullException()
    {
        // Arrange
        Payment? payment = null;

        // Act
        // Assert
        Assert.Throws<ArgumentNullException>(() => BookingMapper.MapToPaymentDto(payment!));
    }

    [Fact]
    public void MapToPaymentDto_WithNullOptionalFields_ShouldMapCorrectly()
    {
        // Arrange
        const long bookingId = 1L;
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
    public void MapToPaymentDto_ShouldMapAllPaymentMethods(PaymentMethod domainMethod, PaymentMethodDto expectedDto)
    {
        // Arrange
        var paymentDate = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var timeProvider = TimeProvider.System;

        var paymentResult = Payment.Create(
            1L,
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
    public void MapToBookingStatusDto_ShouldMapAllValidValues(BookingStatus domain, BookingStatusDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBookingStatusDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToBookingStatusDto_ShouldCoverAllEnumValues()
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
    public void MapToBookingStatusDto_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
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
    public void MapToPaymentStatusDto_ShouldMapAllValidValues(PaymentStatus domain, PaymentStatusDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToPaymentStatusDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToPaymentStatusDto_ShouldCoverAllEnumValues()
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
    public void MapToPaymentStatusDto_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
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
    public void MapToRoomTypeDto_ShouldMapAllValidValues(RoomType domain, RoomTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToRoomTypeDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToRoomTypeDto_ShouldCoverAllEnumValues()
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
    public void MapToRoomTypeDto_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
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
    public void MapToBedTypeDto_ShouldMapAllValidValues(BedType domain, BedTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBedTypeDto(domain);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToBedTypeDto_ShouldCoverAllEnumValues()
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
    public void MapToBedTypeDto_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const BedType invalidValue = (BedType)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => BookingMapper.MapToBedTypeDto(invalidValue));
        Assert.Contains("Invalid bed type value", exception.Message);
    }
}
