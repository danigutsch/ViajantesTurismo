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
}
