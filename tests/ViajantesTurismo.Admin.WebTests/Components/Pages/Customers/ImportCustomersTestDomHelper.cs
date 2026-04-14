using AngleSharp.Dom;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

internal static class ImportCustomersTestDomHelper
{
    internal static IElement FindButtonByText(IRenderedComponent<ImportCustomers> cut, string buttonText)
    {
        return cut.FindAll("button")
            .Single(button => string.Equals(NormalizeText(button.TextContent), buttonText, StringComparison.Ordinal));
    }

    internal static void WaitForEnabledButton(IRenderedComponent<ImportCustomers> cut, string buttonText)
    {
        cut.WaitForAssertion(() => Assert.False(FindButtonByText(cut, buttonText).HasAttribute("disabled")));
    }

    internal static IElement FindRowContainingText(IRenderedComponent<ImportCustomers> cut, string selector, string text)
    {
        return cut.FindAll(selector)
            .Single(row => NormalizeText(row.TextContent).Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeText(string? text)
    {
        return string.Join(' ', (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
