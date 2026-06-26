using System.Text.RegularExpressions;
using static Microsoft.Playwright.Assertions;

namespace ViajantesTurismo.Admin.SystemTests.Shared;

public static class NavigationTestHelpers
{
    public static async Task AssertDeepLink(IPage page, Func<string, Task> navigateTo, string path, string expectedTitle)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(navigateTo);

        await navigateTo(path);
        await Expect(page).ToHaveTitleAsync(expectedTitle);
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
        await Expect(page).ToHaveTitleAsync(expectedTitle);
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
