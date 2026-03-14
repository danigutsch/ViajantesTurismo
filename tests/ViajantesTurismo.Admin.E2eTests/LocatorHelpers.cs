using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests;

/// <summary>
/// Helper class to provide extension methods for locating elements in Playwright tests, improving readability and maintainability of test code.
/// </summary>
internal static class LocatorHelpers
{
    /// <summary>
    /// Provides extension methods for IPage to locate common elements like headings, links, and buttons by their role and name.
    /// </summary>
    /// <param name="page">The IPage instance to extend.</param>
    /// <param name="name">The name of the element to locate.</param>
    /// <returns>An ILocator representing the located element.</returns>
    public static ILocator GetHeading(this IPage page, string name) =>
        page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = name });

    /// <summary>
    /// Provides an extension method for IPage to locate a link by its name, with an optional parameter to specify exact matching.
    /// </summary>
    /// <param name="page">The IPage instance to extend.</param>
    /// <param name="name">The name of the link to locate.</param>
    /// <param name="exact">Whether to match the name exactly.</param>
    /// <returns>An ILocator representing the located link.</returns>
    public static ILocator GetLink(this IPage page, string name, bool? exact = null) =>
        page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = name, Exact = exact });

    /// <summary>
    /// Provides an extension method for IPage to locate a button by its name.
    /// </summary>
    /// <param name="page">The IPage instance to extend.</param>
    /// <param name="name">The name of the button to locate.</param>
    /// <returns>An ILocator representing the located button.</returns>
    public static ILocator GetButton(this IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = name });

    /// <summary>
    /// Provides extension methods for ILocator to locate child elements like headings, links, and buttons by their role and name, allowing for more specific element targeting within a parent locator context.
    /// </summary>
    /// <param name="locator">The ILocator instance to extend.</param>
    /// <param name="name">The name of the heading to locate.</param>
    /// <returns>An ILocator representing the located heading.</returns>
    public static ILocator GetHeading(this ILocator locator, string name) =>
        locator.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = name });

    /// <summary>
    /// Provides an extension method for ILocator to locate a child link by its name, with an optional parameter to specify exact matching, allowing for more specific element targeting within a parent locator context.
    /// </summary>
    /// <param name="locator">The ILocator instance to extend.</param>
    /// <param name="name">The name of the link to locate.</param>
    /// <param name="exact">Whether to match the name exactly.</param>
    /// <returns>An ILocator representing the located link.</returns>
    public static ILocator GetLink(this ILocator locator, string name, bool? exact = null) =>
        locator.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = name, Exact = exact });

    /// <summary>
    /// Provides an extension method for ILocator to locate a child button by its name, allowing for more specific element targeting within a parent locator context.
    /// </summary>
    /// <param name="locator">The ILocator instance to extend.</param>
    /// <param name="name">The name of the button to locate.</param>
    /// <returns>An ILocator representing the located button.</returns>
    public static ILocator GetButton(this ILocator locator, string name) =>
        locator.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = name });

    /// <summary>
    /// Finds a row containing a link with the given href, traversing paginator pages when present.
    /// Returns <c>null</c> if no matching row is found.
    /// </summary>
    /// <param name="page">The current page.</param>
    /// <param name="href">The href to search for (for example, /bookings/{id}).</param>
    /// <param name="tableSelector">Optional table selector. Defaults to "table".</param>
    /// <param name="maxPages">Safety cap for number of pages to inspect.</param>
    /// <param name="retryPasses">Number of full scan passes to retry before giving up.</param>
    private static async Task<ILocator?> FindRowByLinkAcrossPagesAsync(
        this IPage page,
        string href,
        string tableSelector = "table",
        int maxPages = 50,
        int retryPasses = 3)
    {
        var row = page.Locator($"{tableSelector} tbody tr:has(a[href='{href}'])");
        var nextButton = page.Locator(".paginator button[aria-label='Go to next page']");
        var previousButton = page.Locator(".paginator button[aria-label='Go to previous page']");

        for (var pass = 0; pass < retryPasses; pass++)
        {
            if (await row.CountAsync() > 0)
            {
                return row.First;
            }

            if (await previousButton.CountAsync() > 0)
            {
                for (var i = 0; i < maxPages; i++)
                {
                    if (await previousButton.IsDisabledAsync())
                    {
                        break;
                    }

                    await previousButton.ClickAsync();

                    if (await row.CountAsync() > 0)
                    {
                        return row.First;
                    }
                }
            }

            if (await nextButton.CountAsync() > 0)
            {
                for (var i = 0; i < maxPages; i++)
                {
                    if (await nextButton.IsDisabledAsync())
                    {
                        break;
                    }

                    await nextButton.ClickAsync();

                    if (await row.CountAsync() > 0)
                    {
                        return row.First;
                    }
                }
            }

            if (pass < retryPasses - 1)
            {
                await Task.Delay(250);
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a row by link across pages or throws with a descriptive message.
    /// </summary>
    public static async Task<ILocator> RequireRowByLinkAcrossPagesAsync(
        this IPage page,
        string href,
        string tableSelector = "table",
        int maxPages = 50,
        int retryPasses = 3)
    {
        var row = await page.FindRowByLinkAcrossPagesAsync(href, tableSelector, maxPages, retryPasses);
        return row ?? throw new InvalidOperationException($"Could not find row containing link '{href}' within {maxPages} page(s) and {retryPasses} pass(es).");
    }
}
