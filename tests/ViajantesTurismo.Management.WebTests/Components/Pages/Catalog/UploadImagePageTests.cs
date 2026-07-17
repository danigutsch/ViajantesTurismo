using CatalogContractConstants = ViajantesTurismo.Catalog.Contracts.Application.ContractConstants;
using CatalogImageUploadFileReader = ViajantesTurismo.Management.Web.Components.Pages.Catalog.CatalogImageUploadFileReader;
using UploadImage = ViajantesTurismo.Management.Web.Components.Pages.Catalog.UploadImage;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class UploadImagePageTests : BunitContext
{
    private readonly FakeCatalogToursApiClient catalogApi = new();

    public UploadImagePageTests()
    {
        Services.AddSingleton<ICatalogToursApiClient>(catalogApi);
    }

    [Fact]
    public void Displays_the_shared_catalog_upload_limit()
    {
        // Arrange
        var expectedLimitInMebibytes = CatalogContractConstants.MaxMediaUploadBytes / (1024 * 1024);

        // Act
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, Guid.CreateVersion7()));

        // Assert
        cut.Markup.ShouldContain($"Maximum size: {expectedLimitInMebibytes} MiB.", StringComparison.Ordinal);
    }

    [Fact]
    public void Opens_browser_file_reads_with_the_shared_catalog_upload_limit()
    {
        // Arrange
        var file = new RecordingBrowserFile();

        // Act
        using var stream = CatalogImageUploadFileReader.Open(file);

        // Assert
        file.MaximumAllowedSize.ShouldBe(CatalogContractConstants.MaxMediaUploadBytes);
    }

    [Fact]
    public async Task Uploads_selected_image_and_displays_processing_confirmation()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var file = new RecordingBrowserFile();
        catalogApi.UploadedImage = new CatalogMediaImageDto
        {
            Id = Guid.CreateVersion7(),
            AltText = "Cyclists on a mountain pass.",
            ResponsiveVariants = []
        };
        var cut = Render<UploadImage>(parameters => parameters.Add(component => component.Id, tourId));
        var fileInput = cut.FindComponent<InputFile>();

        // Act
        await cut.InvokeAsync(() => fileInput.Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([file])));
        await cut.Find("#altText").ChangeAsync("Cyclists on a mountain pass.");
        await cut.Find("#caption").ChangeAsync("Mountain pass");
        await cut.Find("form").SubmitAsync();
        await cut.WaitForStateAsync(() => cut.FindAll("[role='status']").Count == 1, TimeSpan.FromSeconds(2));

        // Assert
        catalogApi.LastUploadedTourId.ShouldBe(tourId);
        catalogApi.LastUploadedFileName.ShouldBe(file.Name);
        catalogApi.LastUploadedContentType.ShouldBe(file.ContentType);
        catalogApi.LastUploadedAltText.ShouldBe("Cyclists on a mountain pass.");
        catalogApi.LastUploadedCaption.ShouldBe("Mountain pass");
        cut.Find("[role='status']").TextContent.ShouldBe("Image uploaded and awaiting processing.");
        cut.FindAll("[role='alert']").ShouldBeEmpty();
        cut.Find("button[type='submit']").HasAttribute("disabled").ShouldBeTrue();
    }
}
