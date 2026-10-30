using PandaBear.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Browser keyboard journey for cancelling a held Catalog image upload.</summary>
[Trait(PandaBear.Testing.TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class UploadImageKeyboardCancelTests : ManagementWebBrowserTestBase
{
    private readonly FakeCatalogBrowserApiClient catalog = new(CatalogTourBrowserData.DraftTour());

    public UploadImageKeyboardCancelTests()
    {
        Registrations.AddSingleton<ICatalogToursApiClient>(catalog);
    }

    [Fact]
    public async Task Keyboard_cancel_of_a_held_image_upload_reports_canceled_and_focuses_the_status_region()
    {
        // Arrange
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        await BrowserFileSelection.Select(Page, "#imageFile", "cover.png", "image/png", "not-really-a-png");
        await Page.Locator("#altText").FillAsync("A cover image");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Upload image" }).ClickAsync();
        await catalog.UploadStarted;
        var cancel = Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Cancel upload of cover.png" });
        await Expect(cancel).ToBeVisibleAsync();

        // Act
        await Page.Locator("section[aria-label='Catalog image upload status']").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(cancel).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        var observedToken = await catalog.UploadCanceled;
        observedToken.IsCancellationRequested.ShouldBeTrue();
        await Expect(Page.Locator("[data-upload-status]")).ToContainTextAsync("Canceled: cover.png");
        await Expect(Page.Locator("section[aria-label='Catalog image upload status']")).ToBeFocusedAsync();
    }
}
