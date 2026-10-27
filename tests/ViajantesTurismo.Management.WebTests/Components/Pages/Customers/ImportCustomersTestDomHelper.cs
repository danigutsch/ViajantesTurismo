using AngleSharp.Dom;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

/// <summary>DOM lookup helpers for import page component tests.</summary>
internal static class ImportCustomersTestDomHelper
{
    /// <summary>Finds the single button whose whitespace-normalized text equals the given text.</summary>
    internal static IElement FindButtonByText(IRenderedComponent<ImportCustomers> cut, string buttonText)
    {
        return cut.FindAll("button")
            .Single(button => string.Equals(NormalizeText(button.TextContent), buttonText, StringComparison.Ordinal));
    }

    /// <summary>Waits until the button with the given text is not disabled.</summary>
    internal static void WaitForEnabledButton(IRenderedComponent<ImportCustomers> cut, string buttonText)
    {
        cut.WaitForAssertion(() => FindButtonByText(cut, buttonText).HasAttribute("disabled").ShouldBeFalse());
    }

    /// <summary>Finds the single element matching the selector whose normalized text contains the given text, ignoring case.</summary>
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
