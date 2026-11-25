using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Management.Web.Helpers;

/// <summary>
/// The currencies the forms offer, and their names in the selects.
/// </summary>
internal static class CurrencyChoices
{
    /// <summary>Gets the currencies a tour can be priced in, in display order.</summary>
    public static IReadOnlyList<Currency> Tour { get; } = [Currency.Brl, Currency.Eur, Currency.Usd];

    /// <summary>Gets the currencies payments settle in, in display order.</summary>
    public static IReadOnlyList<Currency> Settlement { get; } = [Currency.Eur, Currency.Brl];

    /// <summary>Gets the human-readable name of an ISO 4217 code, or the code itself when it has none.</summary>
    /// <param name="code">The ISO 4217 currency code.</param>
    /// <returns>The display name with its code.</returns>
    public static string DisplayName(string code) => code switch
    {
        "BRL" => "Brazilian Real (BRL)",
        "EUR" => "Euro (EUR)",
        "USD" => "US Dollar (USD)",
        _ => code
    };
}
