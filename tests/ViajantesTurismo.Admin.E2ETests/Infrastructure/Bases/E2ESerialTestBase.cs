using Microsoft.Playwright.Xunit.v3;

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;

/// <summary>
/// Base class for E2E tests that require sequential execution with a clean database.
/// Seeds before each test and clears after. Use for tests that assert exact counts
/// or call ClearDatabase() mid-test.
/// </summary>
[Collection("E2E.Serial")]
public abstract class E2ESerialTestBase(E2EFixture fixture) : PageTest
{
    private static readonly TimeSpan DatabaseResetTimeout = TimeSpan.FromSeconds(30);

    protected HttpClient ApiClient => fixture.ApiClient;

    protected Task ClearDatabase(CancellationToken cancellationToken) => fixture.ClearDatabase(cancellationToken);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await fixture.ClearDatabase(cts.Token);
        await fixture.Seed(cts.Token);
    }

    public override async ValueTask DisposeAsync()
    {
        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await fixture.ClearDatabase(cts.Token);

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

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

[CollectionDefinition("E2E.Serial", DisableParallelization = true)]
public sealed class E2ESerialTests;
