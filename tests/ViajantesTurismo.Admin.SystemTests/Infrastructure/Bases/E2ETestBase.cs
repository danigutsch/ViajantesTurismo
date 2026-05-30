using Microsoft.Playwright.Xunit.v3;
using ViajantesTurismo.Admin.Testing.Integration;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Pages;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Workflows;

[assembly: AssemblyFixture(typeof(E2EFixture))]

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Bases;

public abstract class E2ETestBase(E2EFixture fixture) : PageTest
{
    protected HttpClient ApiClient => fixture.ApiClient;

    protected IAdminTestHost Host => Assert.IsAssignableFrom<IAdminTestHost>(fixture);

    private protected BookingsListPage BookingsList => new(Page, NavigateTo, ApiClient.GetAllBookings);

    private protected BookingWorkflow BookingWorkflow => new(Page, NavigateTo);

    private protected UiFeedbackAssertions UiFeedback => new(Page);

    /// <summary>
    /// Navigate to a path relative to the web app root, waiting for SignalR circuit.
    /// </summary>
    protected async Task NavigateTo(string relativePath)
    {
        await Page.GotoAsync(relativePath, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
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
            BaseURL = fixture.WebAppUrl.ToString(),
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        };
    }
}
