using Microsoft.Playwright.Xunit.v3;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Pages;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Workflows;

[assembly: AssemblyFixture(typeof(E2EFixture))]

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;

public abstract class E2ETestBase(E2EFixture fixture) : PageTest
{
    protected HttpClient ApiClient => fixture.ApiClient;

    private protected BookingsListPage BookingsList => new(Page, NavigateTo, ApiClient.GetAllBookings);

    private protected BookingWorkflow BookingWorkflow => new(Page, NavigateTo);

    /// <summary>
    /// Navigate to a path relative to the web app root, waiting for SignalR circuit.
    /// </summary>
    protected async Task NavigateTo(string relativePath)
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
