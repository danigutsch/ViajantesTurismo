using ViajantesTurismo.AdminApi.Contracts;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.ApiService.Mapping;

/// <summary>
/// Maps Tour-related DTOs to domain objects.
/// </summary>
internal static class TourMapper
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
