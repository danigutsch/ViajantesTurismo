using ViajantesTurismo.Admin.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersPreviewTests : BunitContext
{
    private static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(f => f.Name));

    private static readonly string AllCanonicalValues =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(_ => "v"));

    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersPreviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    private IRenderedComponent<ImportCustomers> GoToPreview(string csvContent, string fileName = "customers.csv")
    {
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(csvContent, fileName);
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));
        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Confirm Import", cut.Markup, StringComparison.Ordinal));
        return cut;
    }

    [Fact]
    public void Clicking_Preview_Advances_To_Preview_Step()
    {
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\n" + AllCanonicalValues, "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("Confirm Import", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void Preview_Step_Shows_File_Name()
    {
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues, "my-data.csv");

        Assert.Contains("my-data.csv", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_Shows_Data_Rows()
    {
        var csvContent = AllCanonicalHeaders + "\n" + AllCanonicalValues + "\n" + AllCanonicalValues;
        var cut = GoToPreview(csvContent);

        var rows = cut.FindAll("table.preview-table tbody tr");
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Preview_Shows_At_Most_Five_Rows()
    {
        var manyRows = string.Join("\n", Enumerable.Repeat(AllCanonicalValues, 10));
        var csvContent = AllCanonicalHeaders + "\n" + manyRows;
        var cut = GoToPreview(csvContent);

        var rows = cut.FindAll("table.preview-table tbody tr");
        Assert.Equal(5, rows.Count);
    }

    [Fact]
    public void Preview_Highlights_Row_With_Empty_Required_Field()
    {
        // FirstName (index 0) is required — a row starting with "," leaves it empty
        var rowWithEmptyFirst = "," + string.Join(",", CustomerImportHeaderMatcher.Fields.Skip(1).Select(_ => "v"));
        var csvContent = AllCanonicalHeaders + "\n" + rowWithEmptyFirst;
        var cut = GoToPreview(csvContent);

        Assert.NotEmpty(cut.FindAll("tr.table-warning"));
    }

    [Fact]
    public void Preview_Row_With_All_Required_Values_Has_No_Warning()
    {
        var csvContent = AllCanonicalHeaders + "\n" + AllCanonicalValues;
        var cut = GoToPreview(csvContent);

        Assert.Empty(cut.FindAll("tr.table-warning"));
    }

    [Fact]
    public void Back_To_Mapping_Returns_To_Header_Mapping_Step()
    {
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);

        cut.Find("button.btn-outline-secondary").Click();

        Assert.DoesNotContain("Confirm Import", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Source Column (CSV)", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_Import_Triggers_Api_Call_And_Shows_Result()
    {
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(1, 0));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("1 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));
    }
}
