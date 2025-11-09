using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Application.Mappings;

/// <summary>
/// Maps Tour-related DTOs to domain objects.
/// </summary>
public static class TourMapper
{
    /// <summary>
    /// Maps a <see cref="CurrencyDto"/> to a <see cref="Currency"/>.
    /// </summary>
    public static Currency MapToCurrency(CurrencyDto currencyDto)
    {
        return currencyDto switch
        {
            CurrencyDto.Real => Currency.Real,
            CurrencyDto.Euro => Currency.Euro,
            CurrencyDto.UsDollar => Currency.UsDollar,
            _ => throw new ArgumentOutOfRangeException(nameof(currencyDto), currencyDto, "Invalid currency value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="Currency"/> to a <see cref="CurrencyDto"/>.
    /// </summary>
    public static CurrencyDto MapToCurrencyDto(Currency currency)
    {
        return currency switch
        {
            Currency.Real => CurrencyDto.Real,
            Currency.Euro => CurrencyDto.Euro,
            Currency.UsDollar => CurrencyDto.UsDollar,
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Invalid currency value.")
        };
    }
}
