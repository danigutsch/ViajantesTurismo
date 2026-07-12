namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Workflows;

internal sealed class ManagementLoginWorkflow(IPage page)
{
    private const string ConformanceUsername = "conformance";
    private const int MaxLoginNavigationAttempts = 5;

    public async Task SignIn(Uri managementWebUrl, string password)
    {
        ArgumentNullException.ThrowIfNull(managementWebUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var loginUrl = new Uri(managementWebUrl, "/login?returnUrl=/");
        await NavigateToLogin(loginUrl);
        await page.Locator("#username").FillAsync(ConformanceUsername);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("#kc-login").ClickAsync();
        await page.WaitForURLAsync(
            url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                   && uri.Host.Equals(managementWebUrl.Host, StringComparison.OrdinalIgnoreCase)
                   && uri.Port == managementWebUrl.Port,
            new PageWaitForURLOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    private async Task NavigateToLogin(Uri loginUrl)
    {
        for (var attempt = 1; attempt <= MaxLoginNavigationAttempts; attempt++)
        {
            try
            {
                await page.GotoAsync(loginUrl.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                return;
            }
            catch (PlaywrightException exception) when (IsOidcRedirect(exception))
            {
                return;
            }
            catch (PlaywrightException exception) when (IsRetryableNetworkChange(exception) && attempt < MaxLoginNavigationAttempts)
            {
                // Aspire can reassign a loopback endpoint while the test fixture is starting.
            }
        }

        throw new InvalidOperationException($"Navigation to '{loginUrl}' did not complete after {MaxLoginNavigationAttempts} attempts.");
    }

    private static bool IsRetryableNetworkChange(PlaywrightException exception)
    {
        return exception.Message.Contains("ERR_NETWORK_CHANGED", StringComparison.Ordinal)
            || exception.Message.Contains("chrome-error://chromewebdata/", StringComparison.Ordinal);
    }

    private static bool IsOidcRedirect(PlaywrightException exception)
    {
        return exception.Message.Contains("is interrupted by another navigation", StringComparison.Ordinal);
    }
}
