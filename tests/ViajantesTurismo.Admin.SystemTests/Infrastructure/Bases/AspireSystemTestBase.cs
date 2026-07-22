using Microsoft.Playwright.Xunit.v3;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Workflows;

[assembly: AssemblyFixture(typeof(AspireSystemTestFixture))]

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Bases;

public abstract class AspireSystemTestBase<TFixture>(TFixture fixture) : PageTest
    where TFixture : IAspireSystemTestFixture
{
    private const float DefaultTimeoutMilliseconds = 30000;
    private const string InteractivePageSelector = ".page[data-interactive=\"true\"]";
    private const string DeveloperExceptionPageSelector = "#stackpage";
    private HttpClient? apiClient;

    protected TFixture Fixture => fixture;

    protected HttpClient ApiClient => apiClient ?? throw new InvalidOperationException("The test API client is not initialized.");

    protected Uri ApiBaseUri => ApiClient.BaseAddress ?? throw new InvalidOperationException("API client base address is not configured.");

    private protected BookingWorkflow BookingWorkflow => new(Page, NavigateTo);

    private protected ManagementLoginWorkflow ManagementLogin => new(Page);

    private protected UiFeedbackAssertions UiFeedback => new(Page);

    protected virtual bool AutomaticallySignIn => true;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        var initialized = false;
        try
        {
            Page.SetDefaultTimeout(DefaultTimeoutMilliseconds);
            Page.SetDefaultNavigationTimeout(DefaultTimeoutMilliseconds);
            Assertions.SetDefaultExpectTimeout(DefaultTimeoutMilliseconds);
            if (AutomaticallySignIn)
            {
                await ManagementLogin.SignIn(Fixture.WebAppUrl, Fixture.ConformanceUserPassword);
            }

            apiClient = await Fixture.CreateApiClient(TestContext.Current.CancellationToken);
            initialized = true;
        }
        finally
        {
            if (!initialized)
            {
                await base.DisposeAsync();
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            apiClient?.Dispose();
            apiClient = null;
        }
        finally
        {
            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    protected async Task NavigateTo(string relativePath)
    {
        await NavigateTo(new Uri(Fixture.WebAppUrl, relativePath));
    }

    protected async Task NavigateTo(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        await Page.GotoAsync(uri.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        if (IsManagementWebAppUri(uri))
        {
            await WaitForInteractivePage();
        }
    }

    private async Task WaitForInteractivePage()
    {
        try
        {
            await Page.Locator(InteractivePageSelector).WaitForAsync(
                new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }
        catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
        {
            if (await Page.Locator(DeveloperExceptionPageSelector).IsVisibleAsync())
            {
                throw new InvalidOperationException(
                    "Management Web returned an ASP.NET developer exception page.",
                    exception);
            }

            throw;
        }
    }

    private bool IsManagementWebAppUri(Uri uri)
    {
        return uri.Scheme.Equals(Fixture.WebAppUrl.Scheme, StringComparison.OrdinalIgnoreCase)
            && uri.Host.Equals(Fixture.WebAppUrl.Host, StringComparison.OrdinalIgnoreCase)
            && uri.Port == Fixture.WebAppUrl.Port;
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
