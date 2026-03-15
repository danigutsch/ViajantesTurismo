namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

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
        var toast = page.Locator(".toast.show");
        await toast.First.WaitForAsync();
        Assert.Contains(expectedText, await toast.First.InnerTextAsync(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a toast is shown, contains the expected text, and then disappears.
    /// </summary>
    /// <param name="expectedText">The expected toast text.</param>
    /// <param name="timeoutMilliseconds">The maximum time to wait for the toast to hide.</param>
    public async Task ExpectToastThenHide(string expectedText, int timeoutMilliseconds = 10_000)
    {
        var toast = page.Locator(".toast").Filter(new LocatorFilterOptions { HasText = expectedText });
        await toast.First.WaitForAsync();
        Assert.Contains(expectedText, await toast.First.InnerTextAsync(), StringComparison.Ordinal);
        await toast.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden,
            Timeout = timeoutMilliseconds
        });
    }

    /// <summary>
    /// Verifies that the standard timed redirect alert is visible and cancellable.
    /// </summary>
    /// <param name="expectedText">The expected redirect message.</param>
    public async Task ExpectRedirectAlert(string expectedText = "Redirecting to bookings page in 3 seconds...")
    {
        var redirectAlert = page.Locator(".alert-info").Filter(new LocatorFilterOptions { HasText = "Redirecting" });
        await redirectAlert.WaitForAsync();
        Assert.Contains(expectedText, await redirectAlert.InnerTextAsync(), StringComparison.Ordinal);
        await redirectAlert.GetButton("Cancel").WaitForAsync();
    }
}
