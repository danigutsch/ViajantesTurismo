using SharedKernel.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Browser keyboard journey for cancelling a held Catalog image upload.</summary>
[Trait(TestTraitNames.AreaName, TestAreas.ManagementWeb)]
[TestScope(TestScope.System)]
[TestHost(TestHost.Browser)]
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
    public async Task A_file_drop_queues_native_browser_streams_and_shows_drag_feedback()
    {
        // Arrange
        catalog.CompleteUploads = true;
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        const string imagePickerLabel = "label[for='imageFile']";
        (string Name, string MediaType, int Length)[] files = [("cover.png", "image/png", 1), ("trail.webp", "image/webp", 2)];

        // Act
        var dragOverCanceled = await BrowserFileSelection.DispatchFileDrag(Page, imagePickerLabel, "dragover", files);

        // Assert
        dragOverCanceled.ShouldBeTrue();
        await Expect(Page.GetByText("Release to select images")).ToBeVisibleAsync();

        // Act
        var dropCanceled = await BrowserFileSelection.DispatchFileDrag(Page, imagePickerLabel, "drop", files);
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });
        await Expect(selected.GetByText("cover.png")).ToBeVisibleAsync();
        await Expect(selected.GetByText("trail.webp")).ToBeVisibleAsync();
        await Page.Locator("#altText").FillAsync("A cover image");
        await Page.Locator("#altText-1").FillAsync("A trail image");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Upload queued images" }).ClickAsync();

        // Assert
        dropCanceled.ShouldBeTrue();
        await Expect(Page.GetByText("Release to select images")).ToBeHiddenAsync();
        await Expect(selected.GetByText("Image uploaded and awaiting processing.")).ToHaveCountAsync(2);
        catalog.UploadedNames.ShouldBe([files[0].Name, files[1].Name]);
    }

    [Fact]
    public async Task A_file_drop_applies_picker_validation_and_prevents_navigation_outside_the_zone()
    {
        // Arrange
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        const int overLimit = (int)ViajantesTurismo.Catalog.Contracts.Application.ContractConstants.MaxMediaUploadBytes + 1;
        (string Name, string MediaType, int Length)[] files = [("notes.txt", "text/plain", 10), ("large.png", "image/png", overLimit)];

        // Act
        var outsideDropCanceled = await BrowserFileSelection.DispatchFileDrag(Page, "h1", "drop", files);
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });

        // Assert
        outsideDropCanceled.ShouldBeTrue();
        await Expect(selected).ToHaveCountAsync(0);

        // Act
        var dropCanceled = await BrowserFileSelection.DispatchFileDrag(Page, "label[for='imageFile']", "drop", files);

        // Assert
        dropCanceled.ShouldBeTrue();
        await Expect(selected.GetByText("Select a JPEG, PNG, WebP, or AVIF image.")).ToBeVisibleAsync();
        await Expect(selected.GetByText("Select an image no larger than 5 MiB.")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Upload queued images" })).ToBeDisabledAsync();
        catalog.UploadStarted.IsCompleted.ShouldBeFalse();
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
