using System.Globalization;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Formats money the way Management Web displays it: in the current UI culture.</summary>
internal static class MoneyText
{
    /// <summary>Formats an amount of an ISO 4217 currency for display.</summary>
    public static string Of(decimal amount, string currency) =>
        Money.Restore(amount, Currency.Restore(currency)).ToString("C", CultureInfo.GetCultureInfo(CultureInfo.CurrentUICulture.Name));
}
