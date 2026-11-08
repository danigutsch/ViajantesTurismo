using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>
/// Maps <see cref="Currency"/> to the representations already stored before the domain adopted the shared ISO 4217
/// type, so existing rows and protected payment state keep loading without a data migration.
/// </summary>
internal static class StoredCurrency
{
    private const string BrlName = "Real";
    private const string EurName = "Euro";
    private const string UsdName = "UsDollar";

    /// <summary>The ISO 4217 "no currency" code that stands for an undefined stored ordinal; the payment domain rejects it on restore.</summary>
    private const string UnknownCode = "XXX";

    /// <summary>Gets the legacy enum member name stored in the tour pricing column.</summary>
    /// <param name="currency">The currency to store.</param>
    /// <returns><c>Real</c>, <c>Euro</c> or <c>UsDollar</c>.</returns>
    /// <exception cref="InvalidOperationException">The currency has no stored name.</exception>
    internal static string ToName(Currency currency) => currency.Code switch
    {
        "BRL" => BrlName,
        "EUR" => EurName,
        "USD" => UsdName,
        _ => throw new InvalidOperationException($"Currency {currency.Code} has no stored representation.")
    };

    /// <summary>Rebuilds a currency from the legacy enum member name stored in the tour pricing column.</summary>
    /// <param name="name">The stored name.</param>
    /// <returns>The currency.</returns>
    /// <exception cref="InvalidOperationException">The name is not a stored currency name.</exception>
    internal static Currency FromName(string name) => name switch
    {
        BrlName => Currency.Brl,
        EurName => Currency.Eur,
        UsdName => Currency.Usd,
        _ => throw new InvalidOperationException($"Stored currency '{name}' is not recognized.")
    };

    /// <summary>Gets the legacy enum ordinal that protected payment state stores.</summary>
    /// <param name="currency">The currency to store.</param>
    /// <returns>0 for BRL, 1 for EUR, 2 for USD.</returns>
    /// <exception cref="InvalidOperationException">The currency has no stored ordinal.</exception>
    internal static int ToOrdinal(Currency currency) => currency.Code switch
    {
        "BRL" => 0,
        "EUR" => 1,
        "USD" => 2,
        _ => throw new InvalidOperationException($"Currency {currency.Code} has no stored representation.")
    };

    /// <summary>Rebuilds a currency from a legacy enum ordinal; an undefined ordinal becomes a currency the domain rejects.</summary>
    /// <param name="ordinal">The stored ordinal.</param>
    /// <returns>The currency.</returns>
    internal static Currency FromOrdinal(int ordinal) => ordinal switch
    {
        0 => Currency.Brl,
        1 => Currency.Eur,
        2 => Currency.Usd,
        _ => Currency.Restore(UnknownCode)
    };
}
