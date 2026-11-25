using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using SharedKernel.Testing;
using ViajantesTurismo.Management.Web.Services;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Keyboard navigation through country selectors on authenticated customer forms.</summary>
[Trait(TestTraitNames.AreaName, TestAreas.ManagementWeb)]
[TestScope(TestScope.System)]
[TestHost(TestHost.Browser)]
public sealed class CountrySelectorKeyboardTests : ManagementWebBrowserTestBase
{
    /// <summary>Provides a fixed set of reference countries to the real form.</summary>
    public CountrySelectorKeyboardTests()
    {
        Registrations.AddSingleton<ICountryService, FakeCountryBrowserService>();
    }

    [Theory]
    [InlineData("/customers/create/personal-info", "#nationality")]
    [InlineData("/customers/create/address", "#country")]
    public async Task Country_options_are_reachable_and_selectable_with_the_keyboard(string path, string selector)
    {
        // Arrange
        await NavigateTo(path);
        var toggle = Page.Locator(selector);
        var search = Page.GetByPlaceholder("Search countries...");
        var option = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Portugal" });

        // Act
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(search).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Tab");

        // Assert
        await Expect(search).ToBeFocusedAsync();

        // Act
        await Page.Keyboard.TypeAsync("Port");
        await Expect(Page.GetByText("Portugal", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(option).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(option).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(toggle).ToBeFocusedAsync();
        await Expect(toggle).ToHaveTextAsync("Portugal");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(search).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Escape_closes_the_country_options_and_returns_focus_without_changing_the_selection()
    {
        // Arrange
        await NavigateTo("/customers/create/personal-info");
        var toggle = Page.Locator("#nationality");
        var search = Page.GetByPlaceholder("Search countries...");
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(search).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Tab");

        // Act
        await Page.Keyboard.PressAsync("Escape");

        // Assert
        await Expect(toggle).ToBeFocusedAsync();
        await Expect(toggle).ToHaveTextAsync("-- Select Country --");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(search).ToHaveCountAsync(0);
    }
}
