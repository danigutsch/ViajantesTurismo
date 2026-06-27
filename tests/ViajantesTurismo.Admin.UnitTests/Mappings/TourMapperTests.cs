using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public class TourMapperTests
{
    [Fact]
    public void Map_to_currency_should_cover_all_enum_values()
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
    public void Map_to_currency_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const CurrencyDto invalidValue = (CurrencyDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => TourMapper.MapToCurrency(invalidValue));
        Assert.Contains("Invalid currency value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_currency_dto_should_cover_all_enum_values()
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
    public void Map_to_currency_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const Currency invalidValue = (Currency)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => TourMapper.MapToCurrencyDto(invalidValue));
        Assert.Contains("Invalid currency value", exception.Message, StringComparison.Ordinal);
    }
}
