using Microsoft.Playwright;
using PandaBear.Testing.Playwright;
namespace ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Workflows;

/// <summary>Signs the conformance user in to the management web app through the Keycloak login page.</summary>
/// <param name="page">The page used for the sign-in.</param>
internal sealed class ManagementLoginWorkflow(IPage page)
{
    private const string ConformanceUsername = "conformance";
    private const int MaxLoginNavigationAttempts = 5;

    /// <summary>Signs in with the given password, retrying up to five times on transient network changes and timeouts, and waits until the browser returns to the management web host.</summary>
    public async Task SignIn(Uri managementWebUrl, string password)
    {
        ArgumentNullException.ThrowIfNull(managementWebUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var loginUrl = new Uri(managementWebUrl, "/login?returnUrl=/");
        for (var attempt = 1; attempt <= MaxLoginNavigationAttempts; attempt++)
        {
            await NavigateToLogin(loginUrl);

            try
            {
                await page.Locator("#username").FillAsync(ConformanceUsername);
                await page.Locator("#password").FillAsync(password);
                await page.Locator("#kc-login").ClickAsync();
                // The login redirects to the returnUrl, the home page. ExpectPath polls the address: WaitForURLAsync can lose a
                // navigation that lands while it subscribes (microsoft/playwright-dotnet#3364).
                await page.ExpectPath(managementWebUrl, "/");
                return;
            }
            catch (Exception exception) when (IsRetryableSignInFailure(exception) && attempt < MaxLoginNavigationAttempts)
            {
                // Retry the complete OIDC redirect after a loopback endpoint change, or when it is interrupted or times out before the
                // web app regains control.
            }
        }

        throw new InvalidOperationException($"Sign-in to '{managementWebUrl}' did not complete after {MaxLoginNavigationAttempts} attempts.");
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

    /// <summary>
    /// Whether a sign-in attempt failed transiently: a timeout, an AppHost loopback endpoint change, a navigation interrupted by the OIDC
    /// redirect, or the post-login address check of <see cref="PlaywrightPageExtensions.ExpectPath(IPage, Uri, string)"/> timing out.
    /// Any other failure, such as a closed page or a missing login field, is real and is not retried.
    /// </summary>
    internal static bool IsRetryableSignInFailure(Exception exception)
    {
        return exception is TimeoutException
            || exception is PlaywrightException playwrightException
            && (IsRetryableNetworkChange(playwrightException)
                || IsOidcRedirect(playwrightException)
                || playwrightException.Message.StartsWith("Page URL expected to be", StringComparison.Ordinal));
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
