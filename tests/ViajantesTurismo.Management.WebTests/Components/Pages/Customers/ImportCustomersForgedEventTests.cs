using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
public sealed class ImportCustomersForgedEventTests : BunitContext
{
    private const string RequiredFieldsMessage = "All required fields must be mapped before importing.";

    private readonly FakeCustomersApiClient fakeCustomersApi = new();

    public ImportCustomersForgedEventTests()
    {
        Services.AddSingleton<PandaBear.Blazor.IUiFailureReporter>(new RecordingUiFailureReporter());
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(fakeCustomersApi);
    }

    [Fact]
    public async Task Forced_preview_and_import_with_required_fields_unmapped_are_refused_in_the_preview_and_the_mapping()
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
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").ClickAsync();

        // Act
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").ClickAsync();

        // Assert
        cut.Find(".alert.alert-danger").TextContent.Trim().ShouldBe(RequiredFieldsMessage);
        importCalls.ShouldBe(0);

        // Act
        await ImportCustomersTestDomHelper.FindButtonByText(cut, "Back to Mapping").ClickAsync();

        // Assert
        cut.Find(".alert.alert-danger").TextContent.Trim().ShouldBe(RequiredFieldsMessage);
        cut.FindAll("select[data-field]").ShouldNotBeEmpty();
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
