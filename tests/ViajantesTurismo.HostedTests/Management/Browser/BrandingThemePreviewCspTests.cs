using Microsoft.Extensions.DependencyInjection;
using PandaBear.Testing;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>
/// Browser journey for the Branding theme preview under the Management Web policy, whose <c>style-src</c> admits only the site
/// and the response's nonce.
/// </summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class BrandingThemePreviewCspTests : ManagementWebBrowserTestBase
{
    private const string PreviewHeading = "#branding-preview .site-theme-preview__h1";

    public BrandingThemePreviewCspTests()
    {
        Registrations.AddSingleton<IBrandingManagementClient>(_ => new BrandingManagementClient(
            new HttpClient(BrandingSettingsStub.Create("BrandName", "Brand name is not accepted.")) { BaseAddress = new Uri("http://branding.invalid") }));
    }

    [Fact]
    public async Task Preview_applies_the_form_colours_under_the_strict_policy_without_a_violation()
    {
        // Arrange
        var recorder = await CspViolationRecorder.Start(Page);
        await NavigateTo("/branding");
        await Expect(Page.Locator(PreviewHeading)).ToHaveCSSAsync("color", "rgb(31, 41, 55)");

        // Act
        await Page.Locator("#branding-text-color").FillAsync("#111827");
        await Page.Locator("#branding-text-color").PressAsync("Tab");

        // Assert
        await Expect(Page.Locator(PreviewHeading)).ToHaveCSSAsync("color", "rgb(17, 24, 39)");
        await Expect(Page.Locator("#branding-preview table.site-theme-preview__contrast")).ToBeVisibleAsync();
        var violations = await recorder.Violations();
        violations.ShouldBeEmpty();
    }
}
