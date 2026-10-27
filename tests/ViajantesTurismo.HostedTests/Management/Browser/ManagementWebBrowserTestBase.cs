using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace ViajantesTurismo.Management.WebBrowserTests;

/// <summary>Base for browser tests that drive Management Web pages hosted in-process against fake API clients.</summary>
public abstract class ManagementWebBrowserTestBase : PageTest
{
    private const string InteractivePageSelector = ".page[data-interactive=\"true\"]";
    private ManagementWebBrowserHost? host;

    /// <summary>Gets the API client registrations the hosted page under test receives; fill it before the test starts.</summary>
    private protected IServiceCollection Registrations { get; } = new ServiceCollection();

    /// <summary>Gets the address of the hosted Management Web.</summary>
    private protected Uri BaseUri => (host ?? throw new InvalidOperationException("The host is not started.")).BaseUri;

    /// <inheritdoc />
    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        host = await ManagementWebBrowserHost.Start(Registrations, TestContext.Current.CancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        try
        {
            if (host is not null)
            {
                await host.DisposeAsync();
            }
        }
        finally
        {
            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Opens a page and waits until its Blazor circuit is interactive.</summary>
    /// <param name="relativePath">The page route.</param>
    private protected async Task NavigateTo(string relativePath)
    {
        await Page.GotoAsync(new Uri(BaseUri, relativePath).ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.Locator(InteractivePageSelector).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}
