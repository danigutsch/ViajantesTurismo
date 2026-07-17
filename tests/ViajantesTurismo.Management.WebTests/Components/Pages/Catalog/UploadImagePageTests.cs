using CatalogContractConstants = ViajantesTurismo.Catalog.Contracts.Application.ContractConstants;
using CatalogImageUploadFileReader = ViajantesTurismo.Management.Web.Components.Pages.Catalog.CatalogImageUploadFileReader;
using UploadImage = ViajantesTurismo.Management.Web.Components.Pages.Catalog.UploadImage;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class UploadImagePageTests : BunitContext
{
    public UploadImagePageTests()
    {
        Services.AddSingleton<ICatalogToursApiClient>(new FakeCatalogToursApiClient());
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
}
