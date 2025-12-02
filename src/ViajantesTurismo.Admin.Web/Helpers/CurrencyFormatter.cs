using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Web.Helpers;

/// <summary>
/// Helper class for formatting currency values with proper symbols based on the currency type.
/// </summary>
public static class CurrencyFormatter
{
    /// <summary>
    /// Formats a decimal amount with the appropriate currency symbol.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="currency">The currency type.</param>
    /// <returns>A formatted string with the currency symbol.</returns>
    public static string Format(decimal amount, CurrencyDto currency)
    {
        var (symbol, position) = currency switch
        {
            CurrencyDto.Real => ("R$", CurrencySymbolPosition.Before),
            CurrencyDto.Euro => ("€", CurrencySymbolPosition.After),
            CurrencyDto.UsDollar => ("$", CurrencySymbolPosition.Before),
            _ => ("¤", CurrencySymbolPosition.Before)
        };

        var formattedAmount = amount.ToString("N2");
        return position == CurrencySymbolPosition.Before
            ? $"{symbol} {formattedAmount}"
            : $"{formattedAmount} {symbol}";
    }

    private enum CurrencySymbolPosition
    {
        Before,
        After
    }
}
