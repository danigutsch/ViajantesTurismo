using static Microsoft.Playwright.Assertions;

namespace ViajantesTurismo.Admin.SystemTests.Shared;

public static class NotFoundPageTestHelpers
{
    public static async Task AssertNotFoundPage(
        IPage page,
        Func<string, Task> navigateTo,
        string path,
        string message,
        string backLink)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(navigateTo);

        await navigateTo(path);
        await Expect(page.GetByText(message)).ToBeVisibleAsync();
        await Expect(page.GetLink(backLink)).ToBeVisibleAsync();
    }

    public static async Task AssertNotFoundPageWithHiddenAction(
        IPage page,
        Func<string, Task> navigateTo,
        string path,
        string message,
        string backLink,
        string hiddenActionLink)
    {
        ArgumentNullException.ThrowIfNull(page);

        await AssertNotFoundPage(page, navigateTo, path, message, backLink);
        await Expect(page.GetLink(hiddenActionLink)).Not.ToBeVisibleAsync();
    }
}
