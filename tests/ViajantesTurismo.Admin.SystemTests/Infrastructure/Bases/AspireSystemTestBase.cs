using Microsoft.Playwright.Xunit.v3;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Pages;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Workflows;

[assembly: AssemblyFixture(typeof(AspireSystemTestFixture))]

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Bases;

public abstract class AspireSystemTestBase<TFixture>(TFixture fixture) : PageTest
    where TFixture : IAspireSystemTestFixture
{
    private const float DefaultTimeoutMilliseconds = 30000;
    private const string InteractivePageSelector = ".page:not([inert])";
    private const string InertPageSelector = ".page[inert]";
    protected TFixture Fixture => fixture;

    protected HttpClient ApiClient => Fixture.ApiClient;

    protected Uri ApiBaseUri => Fixture.ApiBaseUri;

    private protected BookingsListPage BookingsList => new(Page, NavigateTo, ApiClient.GetAllBookings);

    private protected BookingWorkflow BookingWorkflow => new(Page, NavigateTo);

    private protected ManagementLoginWorkflow ManagementLogin => new(Page);

    private protected UiFeedbackAssertions UiFeedback => new(Page);

    protected virtual bool AutomaticallySignIn => true;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        Page.SetDefaultTimeout(DefaultTimeoutMilliseconds);
        Page.SetDefaultNavigationTimeout(DefaultTimeoutMilliseconds);
        Assertions.SetDefaultExpectTimeout(DefaultTimeoutMilliseconds);
        if (AutomaticallySignIn)
        {
            await ManagementLogin.SignIn(Fixture.WebAppUrl, Fixture.ConformanceUserPassword);
        }
    }

    protected async Task NavigateTo(string relativePath)
    {
        await NavigateTo(new Uri(Fixture.WebAppUrl, relativePath));
    }

    protected async Task NavigateTo(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (await TryNavigate(uri, canRetry: attempt < maxAttempts))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Navigation to '{uri}' did not complete after {maxAttempts} attempts.");
    }

    private async Task<bool> TryNavigate(Uri uri, bool canRetry)
    {
        try
        {
            await Page.GotoAsync(uri.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            return !IsManagementWebAppUri(uri) || await WaitForInteractivePage(canRetry);
        }
        catch (PlaywrightException exception) when (IsRetryableNavigationFailure(exception))
        {
            if (IsCurrentRoute(uri))
            {
                return !IsManagementWebAppUri(uri) || await WaitForInteractivePage(canRetry);
            }

            if (!canRetry)
            {
                throw;
            }

            // Retry immediately on transient AppHost network switches instead of relying on a fixed delay.
            return false;
        }
    }

    private async Task<bool> WaitForInteractivePage(bool canRetry)
    {
        try
        {
            await Page.Locator(InteractivePageSelector).WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            return true;
        }
        catch (Exception exception) when (canRetry && exception is PlaywrightException or TimeoutException)
        {
            if (await Page.Locator(InertPageSelector).IsVisibleAsync())
            {
                return false;
            }

            throw;
        }
    }

    private bool IsManagementWebAppUri(Uri uri)
    {
        return uri.Scheme.Equals(Fixture.WebAppUrl.Scheme, StringComparison.OrdinalIgnoreCase)
            && uri.Host.Equals(Fixture.WebAppUrl.Host, StringComparison.OrdinalIgnoreCase)
            && uri.Port == Fixture.WebAppUrl.Port;
    }

    private bool IsCurrentRoute(Uri targetUri)
    {
        return Uri.TryCreate(Page.Url, UriKind.Absolute, out var currentUri)
            && currentUri.Host.Equals(targetUri.Host, StringComparison.OrdinalIgnoreCase)
            && currentUri.Port == targetUri.Port
            && currentUri.PathAndQuery.Equals(targetUri.PathAndQuery, StringComparison.Ordinal);
    }

    private static bool IsRetryableNavigationFailure(PlaywrightException exception) =>
        exception.Message.Contains("ERR_NETWORK_CHANGED", StringComparison.Ordinal)
        || exception.Message.Contains("chrome-error://chromewebdata/", StringComparison.Ordinal)
        || exception.Message.Contains("is interrupted by another navigation", StringComparison.Ordinal);

    protected async Task<string> ReadBookingDetailsBadgeText(Guid bookingId, string label)
    {
        await NavigateTo($"/bookings/{bookingId}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        var badge = Page.GetDetailsBadge(label);
        await Expect(badge).ToBeVisibleAsync();
        return (await badge.InnerTextAsync()).Trim();
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = Fixture.WebAppUrl.ToString(),
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        };
    }
}
