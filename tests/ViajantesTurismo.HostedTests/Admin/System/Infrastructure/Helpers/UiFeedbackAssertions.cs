using Microsoft.Playwright;
namespace ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;

/// <summary>
/// Provides semantic assertions for common toast and redirect feedback in E2E tests.
/// </summary>
/// <param name="page">The active Playwright page.</param>
internal sealed class UiFeedbackAssertions(IPage page)
{
    /// <summary>
    /// Verifies that a toast is visible and contains the expected feedback text.
    /// </summary>
    /// <param name="expectedText">The expected toast text.</param>
    public async Task ExpectToast(string expectedText)
    {
        var toast = page.GetByRole(AriaRole.Alert)
            .Filter(new LocatorFilterOptions { HasText = expectedText });
        await Assertions.Expect(toast).ToBeVisibleAsync();
        await Assertions.Expect(toast).ToContainTextAsync(expectedText);
    }

    /// <summary>
    /// Verifies that a toast is shown, contains the expected text, and then disappears.
    /// </summary>
    /// <param name="expectedText">The expected toast text.</param>
    /// <param name="timeoutMilliseconds">The maximum time to wait for the toast to hide.</param>
    public async Task ExpectToastThenHide(string expectedText, int timeoutMilliseconds = 10_000)
    {
        var toast = page.GetByRole(AriaRole.Alert)
            .Filter(new LocatorFilterOptions { HasText = expectedText });
        await Assertions.Expect(toast).ToBeVisibleAsync();
        await Assertions.Expect(toast).ToContainTextAsync(expectedText);
        await toast.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = timeoutMilliseconds
        });
    }

    /// <summary>
    /// Locates a redirect alert with the exact, visible feedback text.
    /// </summary>
    /// <param name="expectedText">The expected redirect message.</param>
    public ILocator GetRedirectAlert(string expectedText) =>
        page.GetByRole(AriaRole.Alert).Filter(new LocatorFilterOptions
        {
            Has = page.GetByText(expectedText, new PageGetByTextOptions { Exact = true })
                .Filter(new LocatorFilterOptions { Visible = true })
        });
}
