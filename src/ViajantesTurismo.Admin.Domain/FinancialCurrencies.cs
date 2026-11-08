using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.Domain;

/// <summary>Names the subset of ISO 4217 currencies the Admin domain accepts for tours and payments.</summary>
public static class FinancialCurrencies
{
    /// <summary>Determines whether a currency is EUR or BRL, the only currencies obligations and settlements use.</summary>
    /// <param name="currency">The currency to check.</param>
    /// <returns><see langword="true"/> when the currency is EUR or BRL.</returns>
    public static bool IsSupported(Currency? currency) => currency == Currency.Eur || currency == Currency.Brl;

    /// <summary>Determines whether a currency may price a tour: BRL, EUR or USD.</summary>
    /// <param name="currency">The currency to check.</param>
    /// <returns><see langword="true"/> when a tour can be priced in the currency.</returns>
    public static bool IsTourCurrency(Currency? currency) => IsSupported(currency) || currency == Currency.Usd;
}
