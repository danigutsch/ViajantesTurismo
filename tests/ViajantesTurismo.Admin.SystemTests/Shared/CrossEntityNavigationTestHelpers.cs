using static Microsoft.Playwright.Assertions;

namespace ViajantesTurismo.Admin.SystemTests.Shared;

public static class CrossEntityNavigationTestHelpers
{
    public static async Task FollowLinkAndExpectTitle(IPage page, ILocator link, string expectedTitle)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(link);

        await Expect(link).ToBeVisibleAsync();
        await link.ClickAsync();
        await Expect(page).ToHaveTitleAsync(expectedTitle);
    }

    public static async Task ExpectColumnVisibility(ILocator table, string hiddenHeader, string visibleHeader)
    {
        ArgumentNullException.ThrowIfNull(table);

        await Expect(table.Locator($"th:has-text('{hiddenHeader}')")).ToHaveCountAsync(0);
        await Expect(table.Locator($"th:has-text('{visibleHeader}')")).ToBeVisibleAsync();
    }
}
