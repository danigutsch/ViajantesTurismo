using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Pages;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Workflows;

[assembly: AssemblyFixture(typeof(E2EFixture))]

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;

public abstract class E2ETestBase(E2EFixture fixture) : PageTest
{
    protected HttpClient ApiClient => fixture.ApiClient;

    private protected BookingsListPage BookingsList => new(Page, NavigateToAsync, ApiClient.GetAllBookings);

    private protected BookingWorkflow BookingWorkflow => new(Page, NavigateToAsync);

    /// <summary>
    /// Navigate to a path relative to the web app root, waiting for SignalR circuit.
    /// </summary>
    protected async Task NavigateToAsync(string relativePath)
    {
        await Page.GotoAsync(relativePath, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
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
