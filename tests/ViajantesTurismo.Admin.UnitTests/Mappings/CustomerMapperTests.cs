using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Customers;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public class CustomerMapperTests
{
    [Fact]
    public void Map_To_Bike_Type_Dto_Should_Cover_All_Enum_Values()
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

    [Fact]
    public void Map_To_Bed_Type_Should_Cover_All_Enum_Values()
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
    public void Map_To_Bike_Type_Dto_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const BikeType invalidValue = (BikeType)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CustomerMapper.MapToBikeTypeDto(invalidValue));
        Assert.Contains("Invalid bike type value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_To_Bed_Type_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const BedTypeDto invalidValue = (BedTypeDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CustomerMapper.MapToBedType(invalidValue));
        Assert.Contains("Invalid bed type value", exception.Message, StringComparison.Ordinal);
    }
}
