using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Keyboard navigation through the shared layout of a Management form.</summary>
[Trait(TestTraitNames.AreaName, TestAreas.ManagementWeb)]
[TestScope(TestScope.System)]
[TestHost(TestHost.Browser)]
public sealed class ManagementKeyboardNavigationTests : ManagementWebBrowserTestBase
{
    private readonly FakeCatalogBrowserApiClient catalog = new(CatalogTourBrowserData.DraftTour());

    /// <summary>Provides a synthetic tour for the presentation form.</summary>
    public ManagementKeyboardNavigationTests()
    {
        Registrations.AddSingleton<ICatalogToursApiClient>(catalog);
    }

    [Fact]
    public async Task Keyboard_users_can_skip_management_navigation_to_reach_the_form()
    {
        // Arrange
        var path = $"/catalog/tours/{catalog.Tour.Id}/presentation";
        await NavigateTo(path);
        var skipLink = Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Skip to main content" });
        var main = Page.GetByRole(AriaRole.Main);
        var title = main.GetByLabel("Public title", new LocatorGetByLabelOptions { Exact = true });
        await Expect(title).ToHaveValueAsync("Draft Tour");

        // Act
        await skipLink.FocusAsync(new LocatorFocusOptions { Timeout = 5000 });
        await Expect(skipLink).ToBeInViewportAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(main).ToBeFocusedAsync();
        await Expect(title).ToHaveValueAsync("Draft Tour");
        await Expect(Page).ToHaveURLAsync(new Uri(BaseUri, path).ToString());
    }
}
