using ViajantesTurismo.Admin.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersSummaryTests : BunitContext
{
    private static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(f => f.Name));

    private static readonly string AllCanonicalValues =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(_ => "v"));

    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersSummaryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    private IRenderedComponent<ImportCustomers> GoToPreview(string csvContent, string fileName = "customers.csv")
    {
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(csvContent, fileName);
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        return cut;
    }

    private IRenderedComponent<ImportCustomers> ConfirmImportWithoutConflicts(ImportResultDto result)
    {
        _fakeCustomersApi.SetImportCustomersResult(result);
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => Assert.Contains("Import complete.", cut.Markup, StringComparison.Ordinal));
        return cut;
    }

    private static AngleSharp.Dom.IElement FindSuccessSummaryRow(IRenderedComponent<ImportCustomers> cut, string email)
    {
        return ImportCustomersTestDomHelper.FindRowContainingText(cut, "[data-testid='summary-success-rows'] tbody tr", email);
    }

    [Fact]
    public void Confirm_Import_After_Duplicate_Decisions_Shows_Created_Updated_Skipped_And_Failed_Counts()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(2, 1));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        // Act
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "a@example.com")
            .QuerySelector("button[data-action='keep']")!.Click();
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "b@example.com")
            .QuerySelector("button[data-action='overwrite']")!.Click();
        cut.Find("button[data-action='confirm-import']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Created: 1", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Updated: 1", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Skipped: 1", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Failed: 1", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Render_Summary_When_Success_Rows_Contain_Customer_Ids_Shows_View_Customer_Links_For_Created_And_Updated_Rows()
    {
        var createdId = Guid.NewGuid();
        var updatedId = Guid.NewGuid();
        var cut = ConfirmImportWithoutConflicts(
            new ImportResultDto(
                2,
                0,
                null,
                [
                    new ImportSuccessRowDto("created@example.com", "created", createdId),
                    new ImportSuccessRowDto("updated@example.com", "updated", updatedId),
                ]));

        var createdLink = FindSuccessSummaryRow(cut, "created@example.com")
            .QuerySelector("a[data-action='view-customer']");
        var updatedLink = FindSuccessSummaryRow(cut, "updated@example.com")
            .QuerySelector("a[data-action='view-customer']");

        Assert.NotNull(createdLink);
        Assert.NotNull(updatedLink);
        Assert.Contains($"/customers/{createdId}", createdLink.GetAttribute("href"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"/customers/{updatedId}", updatedLink.GetAttribute("href"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_Summary_When_Success_Row_Has_No_Customer_Id_Does_Not_Render_View_Customer_Link()
    {
        var cut = ConfirmImportWithoutConflicts(
            new ImportResultDto(
                1,
                0,
                null,
                [new ImportSuccessRowDto("created@example.com", "created")]));

        Assert.Empty(cut.FindAll("a[data-action='view-customer']"));
        Assert.Single(cut.FindAll("[data-action='customer-id-unavailable']"));
    }

    [Fact]
    public void Render_Summary_When_View_Customer_Link_Is_Available_Targets_Customer_Details_Route()
    {
        var createdId = Guid.NewGuid();
        var cut = ConfirmImportWithoutConflicts(
            new ImportResultDto(
                1,
                0,
                null,
                [new ImportSuccessRowDto("created@example.com", "created", createdId)]));

        var link = FindSuccessSummaryRow(cut, "created@example.com")
            .QuerySelector("a[data-action='view-customer']");

        Assert.NotNull(link);
        Assert.Equal($"/customers/{createdId}", link.GetAttribute("href"));
    }

    [Fact]
    public void Render_Summary_When_Per_Row_Errors_Exist_Shows_Row_And_Field_Level_Error_Messages()
    {
        var cut = ConfirmImportWithoutConflicts(
            new ImportResultDto(
                1,
                2,
                null,
                [new ImportSuccessRowDto("ok@example.com", "created", Guid.NewGuid())],
                [
                    new ImportErrorRowDto(3, "Email", "Email is required", "bad1@example.com"),
                    new ImportErrorRowDto(4, "BirthDate", "BirthDate format is invalid", "bad2@example.com"),
                ]));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Failed rows", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Email is required", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("BirthDate format is invalid", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("bad1@example.com", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("bad2@example.com", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Render_Summary_When_Error_Row_Field_And_Email_Are_Null_Shows_Dash_Placeholders()
    {
        var cut = ConfirmImportWithoutConflicts(
            new ImportResultDto(
                0,
                1,
                null,
                null,
                [new ImportErrorRowDto(3, null, "Unknown validation error")]));

        var row = ImportCustomersTestDomHelper.FindRowContainingText(
            cut,
            "[data-testid='summary-error-rows'] tbody tr",
            "Unknown validation error");
        var cells = row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim()).ToArray();

        Assert.Equal("3", cells[0]);
        Assert.Equal("-", cells[1]);
        Assert.Equal("Unknown validation error", cells[2]);
        Assert.Equal("-", cells[3]);
    }

    [Fact]
    public void Download_Error_Report_When_Error_Rows_Exist_Exports_Current_Error_Rows()
    {
        var cut = ConfirmImportWithoutConflicts(
            new ImportResultDto(
                0,
                1,
                null,
                null,
                [new ImportErrorRowDto(3, "Email", "Email is required", "bad@example.com")]));

        var downloadLink = cut.Find("a[data-action='download-error-report']");
        var href = downloadLink.GetAttribute("href");
        var download = downloadLink.GetAttribute("download");

        Assert.NotNull(href);
        Assert.StartsWith("data:text/csv", href, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("import-errors.csv", download);
    }

    [Fact]
    public void Download_Error_Report_When_Error_Row_Contains_Special_Characters_Escapes_Csv_Values()
    {
        var cut = ConfirmImportWithoutConflicts(
            new ImportResultDto(
                0,
                2,
                null,
                null,
                [
                    new ImportErrorRowDto(3, null, "Unknown validation error"),
                    new ImportErrorRowDto(4, "First,Name", "Value \"quoted\"\nand wrapped", "bad@example.com"),
                ]));

        var downloadLink = cut.Find("a[data-action='download-error-report']");
        var href = downloadLink.GetAttribute("href");

        Assert.NotNull(href);
        var csvPayload = Uri.UnescapeDataString(href.Split(',', 2)[1]);

        Assert.Contains("LineNumber,Field,Message,Email", csvPayload, StringComparison.Ordinal);
        Assert.Contains("3,,Unknown validation error,", csvPayload, StringComparison.Ordinal);
        Assert.Contains("\"First,Name", csvPayload, StringComparison.Ordinal);
        Assert.Contains("\"Value \"\"quoted\"\"", csvPayload, StringComparison.Ordinal);
        Assert.Contains("bad@example.com", csvPayload, StringComparison.Ordinal);
    }

    [Fact]
    public void Retry_Action_After_Summary_Display_Returns_To_Mapping_With_Previous_File_Context()
    {
        var cut = ConfirmImportWithoutConflicts(new ImportResultDto(1, 1));

        cut.Find("button[data-action='retry-current-file']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Source Column (CSV)", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("customers.csv", cut.Markup, StringComparison.Ordinal);
        });
    }
}
