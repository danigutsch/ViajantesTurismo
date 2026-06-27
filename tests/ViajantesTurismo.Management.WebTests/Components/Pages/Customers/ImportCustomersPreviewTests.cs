using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersPreviewTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersPreviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Clicking_preview_advances_to_preview_step()
    {
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues, "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");

        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();

        cut.WaitForAssertion(() => Assert.Contains("Confirm Import", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Preview_step_shows_file_name()
    {
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues, "my-data.csv");

        Assert.Contains("my-data.csv", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_shows_data_rows()
    {
        var csvContent = CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues + "\n" + CustomerImportCsvTestData.AllCanonicalValues;
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, csvContent);

        var rows = cut.FindAll("table.preview-table tbody tr");
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Preview_shows_at_most_five_rows()
    {
        var manyRows = string.Join("\n", Enumerable.Repeat(CustomerImportCsvTestData.AllCanonicalValues, 10));
        var csvContent = CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + manyRows;
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, csvContent);

        var rows = cut.FindAll("table.preview-table tbody tr");
        Assert.Equal(5, rows.Count);
    }

    [Fact]
    public void Preview_highlights_row_with_empty_required_field()
    {
        // FirstName (index 0) is required — a row starting with "," leaves it empty
        var rowWithEmptyFirst = "," + string.Join(",", CustomerImportHeaderMatcher.Fields.Skip(1).Select(_ => "v"));
        var csvContent = CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + rowWithEmptyFirst;
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, csvContent);

        Assert.NotEmpty(cut.FindAll("tr.table-warning"));
    }

    [Fact]
    public void Preview_row_with_all_required_values_has_no_warning()
    {
        var csvContent = CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues;
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, csvContent);

        Assert.Empty(cut.FindAll("tr.table-warning"));
    }

    [Fact]
    public void Back_to_mapping_returns_to_header_mapping_step()
    {
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);

        ImportCustomersTestDomHelper.FindButtonByText(cut, "Back to Mapping").Click();

        Assert.DoesNotContain("Confirm Import", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Source Column (CSV)", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_import_triggers_api_call_and_shows_result()
    {
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(1, 0));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);

        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("1 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));
    }
}
