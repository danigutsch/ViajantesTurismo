using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public class TourMapperTests
{
    [Fact]
    public void Map_To_Currency_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDtoValues = Enum.GetValues<CurrencyDto>();

        foreach (var dtoValue in allDtoValues)
        {
            // Act
            var mappedEnum = TourMapper.MapToCurrency(dtoValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void Map_To_Currency_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const CurrencyDto invalidValue = (CurrencyDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => TourMapper.MapToCurrency(invalidValue));
        Assert.Contains("Invalid currency value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_To_Currency_Dto_Should_Cover_All_Enum_Values()
    {
        // Arrange
        var allDomainValues = Enum.GetValues<Currency>();

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = TourMapper.MapToCurrencyDto(domainValue);

            // Assert
            Assert.True(Enum.IsDefined(mappedEnum));
        }
    }

    [Fact]
    public void Map_To_Currency_Dto_With_Invalid_Value_Should_Throw_Argument_Out_Of_Range_Exception()
    {
        // Arrange
        const Currency invalidValue = (Currency)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => TourMapper.MapToCurrencyDto(invalidValue));
        Assert.Contains("Invalid currency value", exception.Message, StringComparison.Ordinal);
    }
}
