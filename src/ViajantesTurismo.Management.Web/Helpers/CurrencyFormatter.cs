using System.Globalization;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>
/// Presents ISO 4217 currency codes: symbols, display names, and the selectable code lists of the forms.
/// </summary>
internal static class CurrencyFormatter
{
    /// <summary>The ISO 4217 code of the Brazilian Real.</summary>
    public const string Brl = "BRL";

    /// <summary>The ISO 4217 code of the Euro.</summary>
    public const string Eur = "EUR";

    /// <summary>The ISO 4217 code of the US Dollar.</summary>
    public const string Usd = "USD";

    /// <summary>Gets the currencies a tour can be priced in, in display order.</summary>
    public static IReadOnlyList<string> TourCurrencies { get; } = [Brl, Eur, Usd];

    /// <summary>Gets the currencies payments settle in, in display order.</summary>
    public static IReadOnlyList<string> SettlementCurrencies { get; } = [Eur, Brl];

    /// <summary>
    /// Formats a decimal amount with the appropriate currency symbol.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="currency">The ISO 4217 currency code.</param>
    /// <returns>A formatted string with the currency symbol.</returns>
    public static string Format(decimal amount, string currency)
    {
        var symbol = GetSymbol(currency);
        var formattedAmount = amount.ToString("N2", CultureInfo.InvariantCulture);
        return currency == Eur
            ? $"{formattedAmount} {symbol}"
            : $"{symbol} {formattedAmount}";
    }

    /// <summary>
    /// Gets the currency symbol of an ISO 4217 code.
    /// </summary>
    /// <param name="currency">The ISO 4217 currency code.</param>
    /// <returns>The currency symbol, or a generic currency sign for a code without one.</returns>
    public static string GetSymbol(string currency) => currency switch
    {
        Brl => "R$",
        Eur => "€",
        Usd => "$",
        _ => "¤"
    };

    /// <summary>
    /// Gets the human-readable name of an ISO 4217 code.
    /// </summary>
    /// <param name="currency">The ISO 4217 currency code.</param>
    /// <returns>The display name with its code, or the code itself when it has no display name.</returns>
    public static string GetDisplayName(string currency) => currency switch
    {
        Brl => "Brazilian Real (BRL)",
        Eur => "Euro (EUR)",
        Usd => "US Dollar (USD)",
        _ => currency
    };
}
