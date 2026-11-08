using PandaBear.BuildingBlocks;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;

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
            var mapped = TourMapper.MapToCurrency(dtoValue);

            // Assert
            TourMapper.MapToCurrencyDto(mapped).ShouldBe(dtoValue);
        }
    }

    [Fact]
    public void Map_to_currency_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        const CurrencyDto invalidValue = (CurrencyDto)999;

        // Act
        // Assert
        var exception = ShouldAssertionExtensions.ShouldThrow<ArgumentOutOfRangeException>(() => TourMapper.MapToCurrency(invalidValue));
        exception.Message.ShouldContain("Invalid currency value", StringComparison.Ordinal);
    }

    [Fact]
    public void Map_to_currency_dto_should_cover_all_enum_values()
    {
        // Arrange
        Currency[] allDomainValues = [Currency.Brl, Currency.Eur, Currency.Usd];

        foreach (var domainValue in allDomainValues)
        {
            // Act
            var mappedEnum = TourMapper.MapToCurrencyDto(domainValue);

            // Assert
            Enum.IsDefined(mappedEnum).ShouldBeTrue();
            TourMapper.MapToCurrency(mappedEnum).ShouldBe(domainValue);
        }
    }

    [Fact]
    public void Map_to_currency_dto_with_invalid_value_should_throw_argument_out_of_range_exception()
    {
        // Arrange
        var invalidValue = Currency.Restore("XXX");

        // Act
        // Assert
        var exception = ShouldAssertionExtensions.ShouldThrow<ArgumentOutOfRangeException>(() => TourMapper.MapToCurrencyDto(invalidValue));
        exception.Message.ShouldContain("Invalid currency value", StringComparison.Ordinal);
    }
}
