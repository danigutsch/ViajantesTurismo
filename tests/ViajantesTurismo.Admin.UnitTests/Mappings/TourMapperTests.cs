using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.UnitTests.Mappings;

public class TourMapperTests
{
    [Theory]
    [InlineData(CurrencyDto.Real, Currency.Real)]
    [InlineData(CurrencyDto.Euro, Currency.Euro)]
    [InlineData(CurrencyDto.UsDollar, Currency.UsDollar)]
    public void MapToCurrency_ShouldMapAllValidValues(CurrencyDto dto, Currency expected)
    {
        // Arrange
        // Act
        var result = TourMapper.MapToCurrency(dto);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MapToCurrency_ShouldCoverAllEnumValues()
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
    public void MapToCurrency_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const CurrencyDto invalidValue = (CurrencyDto)999;

        // Act
        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => TourMapper.MapToCurrency(invalidValue));
        Assert.Contains("Invalid currency value", exception.Message);
    }
}
