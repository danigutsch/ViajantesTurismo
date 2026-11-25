using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;
using ViajantesTurismo.HostedTests.Catalog.Infrastructure;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Browser tests for progressive Catalog image folder intake.</summary>
[Trait(TestTraitNames.AreaName, "management-web")]
[TestScope(TestScope.System)]
public sealed class UploadImageFolderIntakeTests : ManagementWebBrowserTestBase
{
    private const string AnimationNotice = "The gallery shows only the first frame of animated images.";
    private readonly FakeCatalogBrowserApiClient catalog = new(CatalogTourBrowserData.DraftTour());

    /// <summary>Supplies a Catalog tour and an upload client that reads the browser streams.</summary>
    public UploadImageFolderIntakeTests()
    {
        catalog.CompleteUploads = true;
        Registrations.AddSingleton<ICatalogToursApiClient>(catalog);
    }

    [Fact]
    public async Task A_native_folder_selection_uploads_nested_images_with_only_basename_metadata()
    {
        // Arrange
        using var directory = new TemporaryMediaDirectory();
        var album = Directory.CreateDirectory(Path.Combine(directory.Path, "catalog-test-album"));
        var nested = album.CreateSubdirectory("catalog-test-nested");
        var ct = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(Path.Combine(album.FullName, "cover.png"), "cover", ct);
        await File.WriteAllTextAsync(Path.Combine(nested.FullName, "trail.webp"), "trail", ct);
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var folder = Page.Locator("input[type=file][webkitdirectory]");
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });

        // Act
        await Expect(folder).ToBeVisibleAsync();
        await Expect(folder).ToHaveAccessibleNameAsync("Image folder");
        await Expect(Page.GetByText(AnimationNotice, new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await folder.SetInputFilesAsync(album.FullName);
        await Expect(selected.GetByText("cover.png", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(selected.GetByText("trail.webp", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Page.Locator("#altText").FillAsync("First album image");
        await Page.Locator("#altText-1").FillAsync("Second album image");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Upload queued images" }).ClickAsync();

        // Assert
        await Expect(selected.GetByText("Image uploaded and awaiting processing.")).ToHaveCountAsync(2);
        await Expect(selected.GetByText("catalog-test-", new LocatorGetByTextOptions { Exact = false })).ToHaveCountAsync(0);
        var uploadedNames = catalog.UploadedNames.Order(StringComparer.Ordinal).ToArray();
        uploadedNames.ShouldBe(["cover.png", "trail.webp"]);
    }

    [Fact]
    public async Task A_browser_without_folder_selection_retains_the_native_multi_file_picker()
    {
        // Arrange
        await Page.AddInitScriptAsync("delete HTMLInputElement.prototype.webkitdirectory;");
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var first = new FilePayload { Name = "cover.png", MimeType = "image/png", Buffer = [0x01] };
        var second = new FilePayload { Name = "trail.webp", MimeType = "image/webp", Buffer = [0x02] };

        // Act
        await BrowserFileSelection.Select(Page, "#imageFile", [first, second]);

        // Assert
        var folder = Page.Locator("input[type=file][webkitdirectory]");
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });
        await Expect(folder).ToBeHiddenAsync();
        await Expect(selected.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        catalog.UploadStarted.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task A_dropped_directory_reads_each_page_through_the_empty_page()
    {
        // Arrange
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var zone = Page.Locator("[data-catalog-image-drop-zone]");
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });

        // Act
        await BrowserDirectoryDrop.Dispatch(zone, [["first.png"], ["second.png"]]);

        // Assert
        await Expect(zone).ToHaveAttributeAsync("data-test-directory-reads", "3");
        await Expect(selected.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        await Expect(selected.GetByText("first.png", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(selected.GetByText("second.png", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(zone.GetByText(AnimationNotice, new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        catalog.UploadStarted.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task A_dropped_directory_stops_traversal_when_the_queue_would_exceed_its_file_count()
    {
        // Arrange
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var zone = Page.Locator("[data-catalog-image-drop-zone]");
        var firstPage = Enumerable.Range(1, 10).Select(index => $"image-{index}.png").ToArray();
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });

        // Act
        await BrowserDirectoryDrop.Dispatch(zone, [firstPage, ["extra.png"], ["unvisited.png"]]);

        // Assert
        await Expect(Page.GetByText("Select up to 10 images", new PageGetByTextOptions { Exact = false })
            .And(Page.GetByRole(AriaRole.Alert))).ToBeVisibleAsync();
        await Expect(zone).ToHaveAttributeAsync("data-test-directory-reads", "2");
        await Expect(selected).ToHaveCountAsync(0);
        catalog.UploadStarted.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task A_dropped_directory_stops_traversal_at_an_oversized_file()
    {
        // Arrange
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var zone = Page.Locator("[data-catalog-image-drop-zone]");
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });
        var oversized = checked((int)ContractConstants.MaxMediaUploadBytes + 1);

        // Act
        await BrowserDirectoryDrop.Dispatch(zone, [["oversized.png"], ["unvisited.png"]], oversized);

        // Assert
        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new LocatorFilterOptions
        {
            HasText = "Select an image no larger than 5 MiB."
        })).ToBeVisibleAsync();
        await Expect(zone).ToHaveAttributeAsync("data-test-directory-reads", "1");
        await Expect(selected).ToHaveCountAsync(0);
        catalog.UploadStarted.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task A_dropped_directory_checks_accumulated_bytes_before_handing_files_to_the_queue()
    {
        // Arrange
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var zone = Page.Locator("[data-catalog-image-drop-zone]");
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });
        // Lower the browser aggregate bound to isolate it from the production count and per-file bounds.
        await zone.EvaluateAsync("element => element.dataset.maximumTotalBytes = '6'");

        // Act
        await BrowserDirectoryDrop.Dispatch(zone, [["first.png"], ["second.png"], ["unvisited.png"]]);

        // Assert
        await Expect(Page.GetByRole(AriaRole.Alert).Filter(new LocatorFilterOptions
        {
            HasText = "Select images totaling no more than"
        })).ToBeVisibleAsync();
        await Expect(zone).ToHaveAttributeAsync("data-test-directory-reads", "2");
        await Expect(selected).ToHaveCountAsync(0);
        catalog.UploadStarted.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task A_browser_without_directory_drop_entries_retains_the_flat_drop_path()
    {
        // Arrange
        await Page.AddInitScriptAsync("""
            delete DataTransferItem.prototype.getAsEntry;
            delete DataTransferItem.prototype.webkitGetAsEntry;
            delete DataTransferItem.prototype.getAsFileSystemHandle;
            """);
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });

        // Act
        var dropCanceled = await BrowserFileSelection.DispatchFileDrag(Page, "label[for='imageFile']", "drop",
            [("cover.png", "image/png", 3), ("trail.webp", "image/webp", 4)]);

        // Assert
        dropCanceled.ShouldBeTrue();
        await Expect(selected.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        catalog.UploadStarted.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task A_directory_read_cannot_replace_a_newer_native_file_selection()
    {
        // Arrange
        await NavigateTo($"/catalog/tours/{catalog.Tour.Id}/images/upload");
        var zone = Page.Locator("[data-catalog-image-drop-zone]");
        var selected = Page.GetByRole(AriaRole.List, new PageGetByRoleOptions { Name = "Selected images" });
        await BrowserDirectoryDrop.Dispatch(zone, [["stale.png"]], deferFirstPage: true);
        await Expect(zone).ToHaveAttributeAsync("data-test-directory-reads", "1");

        // Act
        await BrowserFileSelection.Select(Page, "#imageFile", "current.png", "image/png", "test");
        await Expect(selected.GetByText("current.png", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await zone.EvaluateAsync("""
            element => {
                element.dispatchEvent(new Event("test-directory-page-release"));
                return new Promise(requestAnimationFrame);
            }
            """);

        // Assert
        await Expect(zone).ToHaveAttributeAsync("data-test-directory-reads", "1");
        await Expect(selected.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(1);
        await Expect(selected.GetByText("current.png", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
    }
}
