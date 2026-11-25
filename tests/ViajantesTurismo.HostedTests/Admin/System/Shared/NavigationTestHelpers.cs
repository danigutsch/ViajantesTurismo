using Microsoft.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;
using SharedKernel.Testing.Playwright;
using System.Text.RegularExpressions;
using static Microsoft.Playwright.Assertions;

namespace ViajantesTurismo.HostedTests.Admin.System.Shared;

public static class NavigationTestHelpers
{
    /// <summary>Finds the dashboard's list link for a specific route.</summary>
    /// <param name="content">The dashboard content.</param>
    /// <param name="destination">The link's app-relative destination.</param>
    /// <returns>The locator for the matching dashboard link.</returns>
    internal static ILocator GetDashboardListLink(ILocator content, string destination) =>
        content.GetLink("View All", exact: true).And(content.Locator($"a[href='{destination}']"));

    public static async Task AssertDeepLink(IPage page, Func<string, Task> navigateTo, string path, string expectedTitle)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(navigateTo);

        await navigateTo(path);
        await page.ShouldHaveTitle(expectedTitle);
    }

    public static async Task AssertCustomerWizardDeepLink(
        IPage page,
        Func<string, Task> navigateTo,
        Regex expectedUrl,
        string expectedTitle)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(navigateTo);
        ArgumentNullException.ThrowIfNull(expectedUrl);

        await navigateTo("/customers/create");
        await Expect(page).ToHaveURLAsync(expectedUrl);
        await page.ShouldHaveTitle(expectedTitle);
    }

    public static async Task AssertSidebarNavigation(IPage page, ILocator sidebar, string linkName, Regex expectedUrl, bool? exact = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(sidebar);
        ArgumentNullException.ThrowIfNull(expectedUrl);

        await sidebar.GetLink(linkName, exact).ClickAsync();
        await Expect(page).ToHaveURLAsync(expectedUrl);
        await Expect(sidebar.GetLink(linkName, exact))
            .ToHaveClassAsync(NavigationTestRegexes.Active());
    }
}
