using System.Globalization;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;

/// <summary>Formats money the way Management Web displays it: in the UI culture, which the hosted stack inherits from this process.</summary>
internal static class MoneyText
{
    /// <summary>Formats an amount of an ISO 4217 currency for display.</summary>
    public static string Of(decimal amount, string currency) =>
        Money.Restore(amount, Currency.Restore(currency)).ToString("C", CultureInfo.GetCultureInfo(CultureInfo.CurrentUICulture.Name));
}
