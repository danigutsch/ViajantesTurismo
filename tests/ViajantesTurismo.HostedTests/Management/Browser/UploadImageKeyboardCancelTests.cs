using PandaBear.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Browser keyboard journey for cancelling a held Catalog image upload.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class UploadImageKeyboardCancelTests : ManagementWebBrowserTestBase
{
    private readonly FakeCatalogBrowserApiClient catalog = new(CatalogTourBrowserData.DraftTour());

    public UploadImageKeyboardCancelTests()
    {
        Registrations.AddSingleton<ICatalogToursApiClient>(catalog);
    }

    [Fact]
    public async Task Keyboard_users_can_inspect_and_remove_files_from_the_native_multiple_picker()
    {
        // Arrange
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var first = new FilePayload { Name = "cover.png", MimeType = "image/png", Buffer = [0x01] };
        var second = new FilePayload { Name = "trail.webp", MimeType = "image/webp", Buffer = [0x01, 0x02] };

        // Act
        await BrowserFileSelection.Select(Page, "#imageFile", [first, second]);
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });
        await Expect(selected.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        await Expect(selected.GetByText("2 bytes")).ToBeVisibleAsync();
        var remove = selected.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Remove trail.webp" });
        await remove.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // Assert
        await Expect(selected.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(1);
        await Expect(selected.GetByText("cover.png")).ToBeVisibleAsync();
        await Expect(selected.GetByText("trail.webp")).ToHaveCountAsync(0);
        catalog.UploadStarted.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task A_native_multiple_selection_uploads_both_browser_streams_sequentially()
    {
        // Arrange
        catalog.CompleteUploads = true;
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var first = new FilePayload { Name = "cover.png", MimeType = "image/png", Buffer = [0x01] };
        var second = new FilePayload { Name = "trail.webp", MimeType = "image/webp", Buffer = [0x01, 0x02] };
        await BrowserFileSelection.Select(Page, "#imageFile", [first, second]);
        await Page.Locator("#altText").FillAsync("A cover image");
        await Page.Locator("#altText-1").FillAsync("A trail image");

        // Act
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Upload queued images" }).ClickAsync();

        // Assert
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });
        await Expect(selected.GetByText("Image uploaded and awaiting processing.")).ToHaveCountAsync(2);
        catalog.UploadedNames.ShouldBe([first.Name, second.Name]);
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
