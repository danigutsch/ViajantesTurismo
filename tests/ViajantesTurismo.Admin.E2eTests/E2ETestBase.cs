using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using ViajantesTurismo.Admin.E2ETests;

[assembly: AssemblyFixture(typeof(E2EFixture))]

namespace ViajantesTurismo.Admin.E2ETests;

public abstract class E2ETestBase(E2EFixture fixture) : PageTest
{
    protected HttpClient ApiClient => fixture.ApiClient;

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
