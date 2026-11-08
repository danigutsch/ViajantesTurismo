using PandaBear.BuildingBlocks;
using ViajantesTurismo.Admin.Contracts.Application;

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
            CurrencyDto.Real => Currency.Brl,
            CurrencyDto.Euro => Currency.Eur,
            CurrencyDto.UsDollar => Currency.Usd,
            _ => throw new ArgumentOutOfRangeException(nameof(currencyDto), currencyDto, "Invalid currency value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="Currency"/> to a <see cref="CurrencyDto"/>.
    /// </summary>
    public static CurrencyDto MapToCurrencyDto(Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return currency.Code switch
        {
            "BRL" => CurrencyDto.Real,
            "EUR" => CurrencyDto.Euro,
            "USD" => CurrencyDto.UsDollar,
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Invalid currency value.")
        };
    }
}
