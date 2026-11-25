using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Testing.Playwright;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Base for browser tests that drive Management Web pages hosted in-process against fake API clients.</summary>
public abstract class ManagementWebBrowserTestBase : TracedPageTest
{
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
    private protected Task NavigateTo(string relativePath) => GotoInteractive(new Uri(BaseUri, relativePath));
}
