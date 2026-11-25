using SharedKernel.Testing;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Import;

[Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[TestScope(TestScope.Component)]
public sealed class ImportCustomersForgedEventTests : BunitContext
{
    private readonly FakeCustomersApiClient fakeCustomersApi = new();

    public ImportCustomersForgedEventTests()
    {
        Services.AddSingleton<SharedKernel.Blazor.IUiFailureReporter>(new RecordingUiFailureReporter());
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(fakeCustomersApi);
    }

    [Fact]
    public async Task Forced_preview_with_required_fields_unmapped_stays_on_the_mapping_step()
    {
        // Arrange
        var importCalls = 0;
        fakeCustomersApi.SetImportCustomersHandler((_, _, _, _) =>
        {
            Interlocked.Increment(ref importCalls);
            return Task.FromResult(new ImportResultDto(1, 0));
        });
        var cut = Render<ImportCustomers>();
        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("Foo,Bar\n1,2", "customers.csv"));
        cut.WaitForAssertion(() => ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled").ShouldBeTrue());

        // Act
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();

        // Assert
        cut.FindAll("select[data-field]").ShouldNotBeEmpty();
        cut.Markup.ShouldNotContain("Confirm Import", StringComparison.Ordinal);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled").ShouldBeTrue();
        importCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Successful_import_shows_the_complete_status_for_the_file()
    {
        // Arrange
        fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(1, 0));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);

        // Act
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();

        // Assert
        cut.Find("[data-upload-status]").TextContent.ShouldBe("Complete: customers.csv");
    }
}
