using System.Diagnostics.CodeAnalysis;
using Microsoft.Playwright.Xunit.v3;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Bases;

/// <summary>
/// Base class for E2E tests that require sequential execution with a clean database.
/// Applies fixture-owned baseline control before each test and clears state after.
/// Use for tests that assert exact counts or require true clean-slate isolation.
/// </summary>
[Collection(E2ETestCollections.Serial)]
public abstract class E2ESerialTestBase(E2EFixture fixture) : PageTest
{
    private static readonly TimeSpan DatabaseResetTimeout = TimeSpan.FromSeconds(30);

    protected HttpClient ApiClient => fixture.ApiClient;

    protected Task ClearDatabase(CancellationToken cancellationToken) => fixture.ResetDatabase(cancellationToken);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await fixture.ResetDatabase(cts.Token);
        await fixture.SeedBaseline(cts.Token);
    }

    public override async ValueTask DisposeAsync()
    {
        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await fixture.ResetDatabase(cts.Token);

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

/// <summary>
/// Defines the serial E2E test collection and disables parallel execution within it.
/// </summary>
[ExcludeFromCodeCoverage]
[CollectionDefinition(E2ETestCollections.Serial, DisableParallelization = true)]
public sealed class E2ESerialTests : ICollectionFixture<E2EFixture>, ICollectionFixture<AspireSerialSystemTestFixture>;
