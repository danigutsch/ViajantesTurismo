using Microsoft.Playwright.Xunit.v3;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Pages;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Workflows;

[assembly: AssemblyFixture(typeof(AspireSystemTestFixture))]

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Bases;

public abstract class AspireSystemTestBase<TFixture>(TFixture fixture) : PageTest
    where TFixture : IAspireSystemTestFixture
{
    protected TFixture Fixture => fixture;

    protected HttpClient ApiClient => Fixture.ApiClient;

    protected Uri ApiBaseUri => Fixture.ApiBaseUri;

    private protected BookingsListPage BookingsList => new(Page, NavigateTo, ApiClient.GetAllBookings);

    private protected BookingWorkflow BookingWorkflow => new(Page, NavigateTo);

    private protected UiFeedbackAssertions UiFeedback => new(Page);

    protected async Task NavigateTo(string relativePath)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await Page.GotoAsync(relativePath, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                return;
            }
            catch (PlaywrightException exception) when (attempt < maxAttempts
                && exception.Message.Contains("ERR_NETWORK_CHANGED", StringComparison.Ordinal))
            {
                // Retry immediately on transient AppHost network switches instead of relying on a fixed delay.
            }
        }

        throw new InvalidOperationException($"Navigation to '{relativePath}' did not complete after {maxAttempts} attempts.");
    }

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
