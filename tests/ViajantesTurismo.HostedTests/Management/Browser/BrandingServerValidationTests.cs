using PandaBear.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Management.WebBrowserTests;

/// <summary>Browser journey for a server validation problem on the Branding form.</summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class BrandingServerValidationTests : ManagementWebBrowserTestBase
{
    private const string HostileMessage = "<script>window.injectedByServerMessage = true;</script> Brand name is not accepted.";

    public BrandingServerValidationTests()
    {
        Registrations.AddSingleton<IBrandingManagementClient>(_ => new BrandingManagementClient(
            new HttpClient(BrandingSettingsStub.Create("BrandName", HostileMessage)) { BaseAddress = new Uri("http://branding.invalid") }));
    }

    [Fact]
    public async Task Server_validation_problem_shows_an_inert_summary_and_its_link_focuses_the_field_by_keyboard()
    {
        // Arrange
        await NavigateTo("/branding");
        var summary = Page.Locator("#error-summary");
        var brandName = Page.Locator("#branding-brand-name");
        await Expect(brandName).ToHaveValueAsync("Viajantes Turismo");

        // Act
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save branding" }).ClickAsync();

        // Assert
        await Expect(summary).ToBeFocusedAsync();
        var summaryLink = summary.GetByRole(AriaRole.Link);
        await Expect(summaryLink).ToHaveTextAsync(HostileMessage);
        await Expect(Page.Locator(".mb-3", new PageLocatorOptions { Has = brandName }).Locator(".validation-message"))
            .ToHaveTextAsync(HostileMessage);
        await Expect(Page.Locator("#error-summary script, .validation-message script")).ToHaveCountAsync(0);
        var scriptRan = await Page.EvaluateAsync<bool>("() => window.injectedByServerMessage !== undefined");
        scriptRan.ShouldBeFalse();

        // Act
        await Page.Keyboard.PressAsync("Tab");
        await Expect(summaryLink).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(brandName).ToBeFocusedAsync();
        await Expect(Page).ToHaveURLAsync(new Uri(BaseUri, "/branding#branding-brand-name").ToString());
    }
}
