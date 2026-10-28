using PandaBear.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Management.WebBrowserTests;

/// <summary>Browser journey for a server validation problem on the Catalog presentation form.</summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class EditPresentationServerValidationTests : ManagementWebBrowserTestBase
{
    private const string StackTraceText = "at Server.Internal.Handler() in /src/Secret.cs:line 42";
    private static readonly string HostileMessage =
        "<script>window.injectedByServerMessage = true;</script> " + StackTraceText + " " + new string('x', 5000);

    private readonly FakeCatalogBrowserApiClient catalog = new(CatalogTourBrowserData.DraftTour());

    public EditPresentationServerValidationTests()
    {
        catalog.PresentationValidationErrors = new Dictionary<string, string[]>
        {
            ["Title"] = [HostileMessage],
            ["Slug"] = ["Slug is already used."]
        };
        Registrations.AddSingleton<ICatalogToursApiClient>(catalog);
    }

    [Fact]
    public async Task Server_validation_problem_shows_an_inert_capped_summary_and_its_link_focuses_the_field_by_keyboard()
    {
        // Arrange
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/presentation");
        var summary = Page.Locator("#error-summary");
        var title = Page.Locator("#title");
        await Expect(title).ToHaveValueAsync("Draft Tour");

        // Act
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save presentation" }).ClickAsync();

        // Assert
        await Expect(summary).ToBeVisibleAsync();
        await Expect(summary).ToBeFocusedAsync();
        var fieldMessage = Page.Locator(".mb-3", new PageLocatorOptions { Has = title }).Locator(".validation-message");
        await Expect(fieldMessage).ToBeVisibleAsync();
        var fieldText = await fieldMessage.InnerTextAsync();
        fieldText.Length.ShouldBeLessThanOrEqualTo(300);
        var pageText = await Page.Locator("body").InnerTextAsync();
        pageText.ShouldNotContain(StackTraceText, StringComparison.Ordinal);
        await Expect(Page.Locator("#error-summary script, .validation-message script")).ToHaveCountAsync(0);
        var scriptRan = await Page.EvaluateAsync<bool>("() => window.injectedByServerMessage !== undefined");
        scriptRan.ShouldBeFalse();

        // Act
        var summaryLink = summary.GetByRole(AriaRole.Link).First;
        await Page.Keyboard.PressAsync("Tab");
        await Expect(summaryLink).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(title).ToBeFocusedAsync();
        await Expect(Page).ToHaveURLAsync(new Uri(BaseUri, $"/catalog/tours/{catalog.Tour.Id}/presentation#title").ToString());
    }
}
