using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.Testing.Fakes.ApiClients;
using ViajantesTurismo.Management.Web.Services;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Verifies the heading landmarks of the authenticated customer wizard.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class CustomerWizardHeadingTests : ManagementWebBrowserTestBase
{
    /// <summary>Provides isolated dependencies for the wizard routes.</summary>
    public CustomerWizardHeadingTests()
    {
        Registrations.AddSingleton<ICountryService, FakeCountryBrowserService>();
        Registrations.AddSingleton<ICustomersApiClient, FakeCustomersApiClient>();
    }

    [Theory]
    [InlineData("personal-info", "Step 1 of 8: Personal Information")]
    [InlineData("identification", "Step 2 of 8: Identification Information")]
    [InlineData("contact", "Step 3 of 8: Contact Information")]
    [InlineData("address", "Step 4 of 8: Address")]
    [InlineData("physical", "Step 5 of 8: Physical Information")]
    [InlineData("accommodation", "Step 6 of 8: Accommodation Preferences")]
    [InlineData("emergency-contact", "Step 7 of 8: Emergency Contact")]
    [InlineData("medical", "Step 8 of 8: Medical Information")]
    public async Task Customer_wizard_steps_expose_a_page_heading_and_an_ordered_step_heading(string step, string stepTitle)
    {
        // Arrange
        var main = Page.GetByRole(AriaRole.Main);
        var primaryHeading = main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Level = 1 });

        // Act
        await NavigateTo($"/customers/create/{step}");

        // Assert
        await Expect(primaryHeading).ToHaveTextAsync("Create New Customer");
        await Expect(main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Level = 2, Name = stepTitle })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Customer_review_exposes_a_page_heading_even_when_information_is_incomplete()
    {
        // Arrange
        var main = Page.GetByRole(AriaRole.Main);
        var heading = main.GetByRole(AriaRole.Heading, new LocatorGetByRoleOptions { Level = 1 });

        // Act
        await NavigateTo("/customers/create/review");

        // Assert
        await Expect(heading).ToHaveTextAsync("Create New Customer");
    }

    [Fact]
    public async Task Returning_from_review_focuses_the_page_heading_on_the_first_step()
    {
        // Arrange
        await NavigateTo("/customers/create/review");
        var firstStep = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Go to Step 1" });

        // Act
        await firstStep.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(Page).ToHaveURLAsync(new Uri(BaseUri, "/customers/create/personal-info").ToString());
        var heading = Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Level = 1, Name = "Create New Customer" });
        await Expect(heading).ToBeFocusedAsync();
    }
}
