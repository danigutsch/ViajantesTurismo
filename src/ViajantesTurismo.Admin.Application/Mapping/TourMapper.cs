using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Application.Mapping;

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
}
