using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace ViajantesTurismo.Admin.E2ETests;

[Collection("E2E")]
public abstract class E2ETestBase(E2EFixture fixture) : PageTest
{
    protected E2EFixture Fixture { get; } = fixture;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await Fixture.Seed(cts.Token);
    }

    public override async ValueTask DisposeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await Fixture.ClearDatabase(cts.Token);

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

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
            BaseURL = Fixture.WebAppUrl.ToString(),
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        };
    }
}

[CollectionDefinition("E2E")]
public sealed class AdminE22Tests : ICollectionFixture<E2EFixture>;
