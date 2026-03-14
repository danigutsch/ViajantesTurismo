namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

/// <summary>
/// Helper class to provide extension methods for locating elements in Playwright tests, improving readability and maintainability of test code.
/// </summary>
internal static class LocatorHelpers
{
    /// <param name="page">The IPage instance to extend.</param>
    extension(IPage page)
    {
        /// <summary>
        /// Provides extension methods for IPage to locate common elements like headings, links, and buttons by their role and name.
        /// </summary>
        /// <param name="name">The name of the element to locate.</param>
        /// <returns>An ILocator representing the located element.</returns>
        public ILocator GetHeading(string name) =>
            page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = name });

        /// <summary>
        /// Provides an extension method for IPage to locate a link by its name, with an optional parameter to specify exact matching.
        /// </summary>
        /// <param name="name">The name of the link to locate.</param>
        /// <param name="exact">Whether to match the name exactly.</param>
        /// <returns>An ILocator representing the located link.</returns>
        public ILocator GetLink(string name, bool? exact = null) =>
            page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = name, Exact = exact });

        /// <summary>
        /// Provides an extension method for IPage to locate a button by its name.
        /// </summary>
        /// <param name="name">The name of the button to locate.</param>
        /// <returns>An ILocator representing the located button.</returns>
        public ILocator GetButton(string name) =>
            page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = name });

        /// <summary>
        /// Cancels the timed redirect affordance when it is present on the page.
        /// </summary>
        public async Task CancelTimedRedirect()
        {
            var cancelButton = page.Locator(".alert-info button", new PageLocatorOptions { HasText = "Cancel" });
            if (await cancelButton.CountAsync() == 0)
            {
                return;
            }

            await cancelButton.ClickAsync();
        }
    }

    /// <param name="locator">The ILocator instance to extend.</param>
    extension(ILocator locator)
    {
        /// <summary>
        /// Provides extension methods for ILocator to locate child elements like headings, links, and buttons by their role and name, allowing for more specific element targeting within a parent locator context.
        /// </summary>
        /// <param name="name">The name of the heading to locate.</param>
        /// <returns>An ILocator representing the located heading.</returns>
        public ILocator GetHeading(string name) =>
            locator.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Name = name });

        /// <summary>
        /// Provides an extension method for ILocator to locate a child link by its name, with an optional parameter to specify exact matching, allowing for more specific element targeting within a parent locator context.
        /// </summary>
        /// <param name="name">The name of the link to locate.</param>
        /// <param name="exact">Whether to match the name exactly.</param>
        /// <returns>An ILocator representing the located link.</returns>
        public ILocator GetLink(string name, bool? exact = null) =>
            locator.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = name, Exact = exact });

        /// <summary>
        /// Provides an extension method for ILocator to locate a child button by its name, allowing for more specific element targeting within a parent locator context.
        /// </summary>
        /// <param name="name">The name of the button to locate.</param>
        /// <returns>An ILocator representing the located button.</returns>
        public ILocator GetButton(string name) =>
            locator.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = name });
    }
}
