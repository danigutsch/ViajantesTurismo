using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public class CustomerMapperTests
{
    [Theory]
    [InlineData(BikeType.None, BikeTypeDto.None)]
    [InlineData(BikeType.Regular, BikeTypeDto.Regular)]
    [InlineData(BikeType.EBike, BikeTypeDto.EBike)]
    public void MapToBikeTypeDto_ShouldMapAllValidValues(BikeType domain, BikeTypeDto expected)
    {
        // Arrange
        // Act
        var result = CustomerMapper.MapToBikeTypeDto(domain);

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
            var mappedEnum = CustomerMapper.MapToBikeTypeDto(domainValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Theory]
    [InlineData(BedTypeDto.SingleBed, BedType.SingleBed)]
    [InlineData(BedTypeDto.DoubleBed, BedType.DoubleBed)]
    public void MapToBedType_ShouldMapAllValidValues(BedTypeDto dto, BedType expected)
    {
        // Arrange
        // Act
        var result = CustomerMapper.MapToBedType(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToBedType_ShouldCoverAllEnumValues()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<BedTypeDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = CustomerMapper.MapToBedType(dtoValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void MapToBikeTypeDto_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const BikeType invalidValue = (BikeType)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CustomerMapper.MapToBikeTypeDto(invalidValue));
        Assert.Contains("Invalid bike type value", exception.Message);
    }

    [Fact]
    public void MapToBedType_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const BedTypeDto invalidValue = (BedTypeDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CustomerMapper.MapToBedType(invalidValue));
        Assert.Contains("Invalid bed type value", exception.Message);
    }
}
