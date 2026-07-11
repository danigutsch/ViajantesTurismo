using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Bookings.Mappings;

public class BookingMapperTests
{
    [Fact]
    public void Map_to_bike_type_should_cover_all_enum_values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<BikeTypeDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBikeType(dtoValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_bike_type_dto_should_cover_all_enum_values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<BikeType>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBikeTypeDto(domainValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_room_type_should_cover_all_enum_values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<RoomTypeDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToRoomType(dtoValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_booking_status_should_cover_all_enum_values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<BookingStatusDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBookingStatus(dtoValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_payment_status_should_cover_all_enum_values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<PaymentStatusDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToPaymentStatus(dtoValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_discount_type_should_cover_all_enum_values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<DiscountTypeDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToDiscountType(dtoValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_discount_type_dto_should_cover_all_enum_values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<DiscountType>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToDiscountTypeDto(domainValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_bike_type_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const BikeTypeDto invalidValue = (BikeTypeDto)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToBikeType(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid bike type value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_bike_type_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const BikeType invalidValue = (BikeType)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToBikeTypeDto(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid bike type value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_room_type_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const RoomTypeDto invalidValue = (RoomTypeDto)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToRoomType(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid room type value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_booking_status_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const BookingStatusDto invalidValue = (BookingStatusDto)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToBookingStatus(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid booking status value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_payment_status_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const PaymentStatusDto invalidValue = (PaymentStatusDto)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToPaymentStatus(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid payment status value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_discount_type_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const DiscountTypeDto invalidValue = (DiscountTypeDto)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToDiscountType(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid discount type value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_discount_type_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const DiscountType invalidValue = (DiscountType)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToDiscountTypeDto(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid discount type value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_payment_method_should_cover_all_enum_values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<PaymentMethodDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToPaymentMethod(dtoValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_payment_method_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const PaymentMethodDto invalidValue = (PaymentMethodDto)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToPaymentMethod(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid payment method value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_payment_method_dto_should_cover_all_enum_values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<PaymentMethod>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToPaymentMethodDto(domainValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_payment_method_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const PaymentMethod invalidValue = (PaymentMethod)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToPaymentMethodDto(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid payment method value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_payment_dto_should_map_all_properties()
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

        (paymentResult.IsSuccess).ShouldBeTrue();
        var payment = paymentResult.Value;

        // Act
        var result = BookingMapper.MapToPaymentDto(payment);

        // Assert
        (result.Id).ShouldBe(payment.Id);
        (result.BookingId).ShouldBe(bookingId);
        (result.Amount).ShouldBe(150.50m);
        (result.PaymentDate).ShouldBe(paymentDate);
        (result.Method).ShouldBe(PaymentMethodDto.CreditCard);
        (result.ReferenceNumber).ShouldBe("REF-12345");
        (result.Notes).ShouldBe("Payment for tour booking");
        (result.RecordedAt).ShouldBe(payment.RecordedAt);
    }

    [Fact]
    public void Map_to_payment_dto_with_null_payment_should_throw_argument_null_exception()
    {
        // Arrange
        Payment? payment = null;

        // Act
        // Assert
        ((Func<object?>)(() => BookingMapper.MapToPaymentDto(payment!))).ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Map_to_payment_dto_with_null_optional_fields_should_map_correctly()
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

        (paymentResult.IsSuccess).ShouldBeTrue();
        var payment = paymentResult.Value;

        // Act
        var result = BookingMapper.MapToPaymentDto(payment);

        // Assert
        (result.Id).ShouldBe(payment.Id);
        (result.BookingId).ShouldBe(bookingId);
        (result.Amount).ShouldBe(100.00m);
        (result.PaymentDate).ShouldBe(paymentDate);
        (result.Method).ShouldBe(PaymentMethodDto.Cash);
        (result.ReferenceNumber).ShouldBeNull();
        (result.Notes).ShouldBeNull();
        (result.RecordedAt).ShouldBe(payment.RecordedAt);
    }

    [Theory]
    [InlineData(PaymentMethod.CreditCard, PaymentMethodDto.CreditCard)]
    [InlineData(PaymentMethod.BankTransfer, PaymentMethodDto.BankTransfer)]
    [InlineData(PaymentMethod.Cash, PaymentMethodDto.Cash)]
    [InlineData(PaymentMethod.Check, PaymentMethodDto.Check)]
    [InlineData(PaymentMethod.PayPal, PaymentMethodDto.PayPal)]
    [InlineData(PaymentMethod.Other, PaymentMethodDto.Other)]
    public void Map_to_payment_dto_should_map_all_payment_methods(PaymentMethod domainMethod, PaymentMethodDto expectedDto)
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

        (paymentResult.IsSuccess).ShouldBeTrue();
        var payment = paymentResult.Value;

        // Act
        var result = BookingMapper.MapToPaymentDto(payment);

        // Assert
        (result.Method).ShouldBe(expectedDto);
    }

    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatusDto.Pending)]
    [InlineData(BookingStatus.Confirmed, BookingStatusDto.Confirmed)]
    [InlineData(BookingStatus.Cancelled, BookingStatusDto.Cancelled)]
    [InlineData(BookingStatus.Completed, BookingStatusDto.Completed)]
    public void Map_to_booking_status_dto_should_map_all_valid_values(BookingStatus domain, BookingStatusDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBookingStatusDto(domain);

        // Assert
        (result).ShouldBe(expected);
    }

    [Fact]
    public void Map_to_booking_status_dto_should_cover_all_enum_values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<BookingStatus>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBookingStatusDto(domainValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_booking_status_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const BookingStatus invalidValue = (BookingStatus)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToBookingStatusDto(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid booking status value", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PaymentStatus.Unpaid, PaymentStatusDto.Unpaid)]
    [InlineData(PaymentStatus.PartiallyPaid, PaymentStatusDto.PartiallyPaid)]
    [InlineData(PaymentStatus.Paid, PaymentStatusDto.Paid)]
    [InlineData(PaymentStatus.Refunded, PaymentStatusDto.Refunded)]
    public void Map_to_payment_status_dto_should_map_all_valid_values(PaymentStatus domain, PaymentStatusDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToPaymentStatusDto(domain);

        // Assert
        (result).ShouldBe(expected);
    }

    [Fact]
    public void Map_to_payment_status_dto_should_cover_all_enum_values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<PaymentStatus>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToPaymentStatusDto(domainValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_payment_status_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const PaymentStatus invalidValue = (PaymentStatus)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToPaymentStatusDto(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid payment status value", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(RoomType.DoubleOccupancy, RoomTypeDto.DoubleOccupancy)]
    [InlineData(RoomType.SingleOccupancy, RoomTypeDto.SingleOccupancy)]
    public void Map_to_room_type_dto_should_map_all_valid_values(RoomType domain, RoomTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToRoomTypeDto(domain);

        // Assert
        (result).ShouldBe(expected);
    }

    [Fact]
    public void Map_to_room_type_dto_should_cover_all_enum_values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<RoomType>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToRoomTypeDto(domainValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_room_type_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const RoomType invalidValue = (RoomType)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToRoomTypeDto(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid room type value", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(BedType.SingleBed, BedTypeDto.SingleBed)]
    [InlineData(BedType.DoubleBed, BedTypeDto.DoubleBed)]
    public void Map_to_bed_type_dto_should_map_all_valid_values(BedType domain, BedTypeDto expected)
    {
        // Arrange
        // Act
        var result = BookingMapper.MapToBedTypeDto(domain);

        // Assert
        (result).ShouldBe(expected);
    }

    [Fact]
    public void Map_to_bed_type_dto_should_cover_all_enum_values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<BedType>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = BookingMapper.MapToBedTypeDto(domainValue);

            // Assert
            (Enum.IsDefined(mappedEnum)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Map_to_bed_type_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const BedType invalidValue = (BedType)999;

        // Act
        // Assert
        var exception = ((Func<object?>)(() => BookingMapper.MapToBedTypeDto(invalidValue))).ShouldThrow<ArgumentOutOfRangeException>();
        (exception.Message).ShouldContain("Invalid bed type value", StringComparison.Ordinal);
    }
}
