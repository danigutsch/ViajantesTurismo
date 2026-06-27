using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public class CustomerMapperTests
{
    [Fact]
    public void Map_to_bike_type_dto_should_cover_all_enum_values()
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
    public void Map_to_bed_type_should_cover_all_enum_values()
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
    public void Map_to_bike_type_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const BikeType invalidValue = (BikeType)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CustomerMapper.MapToBikeTypeDto(invalidValue));
        Assert.Contains("Invalid bike type value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_bed_type_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const BedTypeDto invalidValue = (BedTypeDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CustomerMapper.MapToBedType(invalidValue));
        Assert.Contains("Invalid bed type value", exception.Message, StringComparison.Ordinal);
    }
}
