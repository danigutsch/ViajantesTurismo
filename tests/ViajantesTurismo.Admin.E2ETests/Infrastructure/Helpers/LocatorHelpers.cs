namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

/// <summary>
/// Helper class to provide extension methods for locating elements in Playwright tests, improving readability and maintainability of test code.
/// </summary>
internal static class LocatorHelpers
{
    /// <summary>
    /// Provides extension methods for IPage to locate common elements like headings, links, buttons, and details badges.
    /// </summary>
    /// <param name="page">The IPage instance to extend.</param>
    /// <param name="name">The name of the element to locate.</param>
    /// <returns>An ILocator representing the located element.</returns>
    public static ILocator GetHeading(this IPage page, string name) =>
        page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = name });

    /// <summary>
    /// Provides extension methods for ILocator to locate child elements like headings, links, and buttons by their role and name, allowing for more specific element targeting within a parent locator context.
    /// </summary>
    /// <param name="locator">The ILocator instance to extend.</param>
    /// <param name="name">The name of the heading to locate.</param>
    /// <returns>An ILocator representing the located heading.</returns>
    public static ILocator GetHeading(this ILocator locator, string name) =>
        locator.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = name });

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
    /// Provides an extension method for ILocator to locate a child link by its name, with an optional parameter to specify exact matching, allowing for more specific element targeting within a parent locator context.
    /// </summary>
    /// <param name="locator">The ILocator instance to extend.</param>
    /// <param name="name">The name of the link to locate.</param>
    /// <param name="exact">Whether to match the name exactly.</param>
    /// <returns>An ILocator representing the located link.</returns>
    public static ILocator GetLink(this ILocator locator, string name, bool? exact = null) =>
        locator.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = name, Exact = exact });

    /// <summary>
    /// Provides an extension method for IPage to locate a button by its name.
    /// </summary>
    /// <param name="page">The IPage instance to extend.</param>
    /// <param name="name">The name of the button to locate.</param>
    /// <returns>An ILocator representing the located button.</returns>
    public static ILocator GetButton(this IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = name });

    /// <summary>
    /// Provides an extension method for ILocator to locate a child button by its name, allowing for more specific element targeting within a parent locator context.
    /// </summary>
    /// <param name="locator">The ILocator instance to extend.</param>
    /// <param name="name">The name of the button to locate.</param>
    /// <returns>An ILocator representing the located button.</returns>
    public static ILocator GetButton(this ILocator locator, string name) =>
        locator.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = name });

    /// <summary>
    /// Locates the badge displayed in the definition list row for the provided label.
    /// </summary>
    /// <param name="page">The IPage instance to extend.</param>
    /// <param name="label">The definition term label.</param>
    /// <returns>The badge locator associated with the label.</returns>
    public static ILocator GetDetailsBadge(this IPage page, string label)
    {
        var term = page.Locator($"dt:text-is('{label}')");
        return term.Locator("xpath=following-sibling::dd[1]").Locator(".badge");
    }

    /// <summary>
    /// Cancels the timed redirect affordance when it is present on the page.
    /// </summary>
    /// <param name="page">The IPage instance to extend.</param>
    public static async Task CancelTimedRedirect(this IPage page)
    {
        var cancelButton = page.Locator(".alert-info button", new PageLocatorOptions { HasText = "Cancel" });
        if (await cancelButton.CountAsync() == 0)
        {
            return;
        }

        await cancelButton.ClickAsync();
    }
}
