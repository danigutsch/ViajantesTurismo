namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersSummaryTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersSummaryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Confirm_import_after_duplicate_decisions_shows_created_updated_skipped_and_failed_counts()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(2, 1));
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(this, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);
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
    public void Render_summary_when_success_rows_contain_customer_ids_shows_view_customer_links_for_created_and_updated_rows()
    {
        var createdId = Guid.NewGuid();
        var updatedId = Guid.NewGuid();
        var cut = ImportCustomersSummaryTestsHelpers.ConfirmImportWithoutConflicts(this, _fakeCustomersApi,
            new ImportResultDto(
                2,
                0,
                null,
                [
                    new ImportSuccessRowDto("created@example.com", "created", createdId),
                    new ImportSuccessRowDto("updated@example.com", "updated", updatedId),
                ]));

        var createdLink = ImportCustomersSummaryTestsHelpers.FindSuccessSummaryRow(cut, "created@example.com")
            .QuerySelector("a[data-action='view-customer']");
        var updatedLink = ImportCustomersSummaryTestsHelpers.FindSuccessSummaryRow(cut, "updated@example.com")
            .QuerySelector("a[data-action='view-customer']");

        Assert.NotNull(createdLink);
        Assert.NotNull(updatedLink);
        Assert.Contains($"/customers/{createdId}", createdLink.GetAttribute("href"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"/customers/{updatedId}", updatedLink.GetAttribute("href"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_summary_when_success_row_has_no_customer_id_does_not_render_view_customer_link()
    {
        var cut = ImportCustomersSummaryTestsHelpers.ConfirmImportWithoutConflicts(this, _fakeCustomersApi,
            new ImportResultDto(
                1,
                0,
                null,
                [new ImportSuccessRowDto("created@example.com", "created")]));

        Assert.Empty(cut.FindAll("a[data-action='view-customer']"));
        Assert.Single(cut.FindAll("[data-action='customer-id-unavailable']"));
    }

    [Fact]
    public void Render_summary_when_view_customer_link_is_available_targets_customer_details_route()
    {
        var createdId = Guid.NewGuid();
        var cut = ImportCustomersSummaryTestsHelpers.ConfirmImportWithoutConflicts(this, _fakeCustomersApi,
            new ImportResultDto(
                1,
                0,
                null,
                [new ImportSuccessRowDto("created@example.com", "created", createdId)]));

        var link = ImportCustomersSummaryTestsHelpers.FindSuccessSummaryRow(cut, "created@example.com")
            .QuerySelector("a[data-action='view-customer']");

        Assert.NotNull(link);
        Assert.Equal($"/customers/{createdId}", link.GetAttribute("href"));
    }

    [Fact]
    public void Render_summary_when_per_row_errors_exist_shows_row_and_field_level_error_messages()
    {
        var cut = ImportCustomersSummaryTestsHelpers.ConfirmImportWithoutConflicts(this, _fakeCustomersApi,
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
    public void Render_summary_when_error_row_field_and_email_are_null_shows_dash_placeholders()
    {
        var cut = ImportCustomersSummaryTestsHelpers.ConfirmImportWithoutConflicts(this, _fakeCustomersApi,
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
    public void Download_error_report_when_error_rows_exist_exports_current_error_rows()
    {
        var cut = ImportCustomersSummaryTestsHelpers.ConfirmImportWithoutConflicts(this, _fakeCustomersApi,
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
    public void Download_error_report_when_error_row_contains_special_characters_escapes_csv_values()
    {
        var cut = ImportCustomersSummaryTestsHelpers.ConfirmImportWithoutConflicts(this, _fakeCustomersApi,
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
    public void Retry_action_after_summary_display_returns_to_mapping_with_previous_file_context()
    {
        var cut = ImportCustomersSummaryTestsHelpers.ConfirmImportWithoutConflicts(this, _fakeCustomersApi, new ImportResultDto(1, 1));

        cut.Find("button[data-action='retry-current-file']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Source Column (CSV)", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("customers.csv", cut.Markup, StringComparison.Ordinal);
        });
    }

}
