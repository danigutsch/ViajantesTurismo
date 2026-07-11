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
        cut.WaitForAssertion(() => (cut.Markup).ShouldContain("Resolve Duplicates", StringComparison.Ordinal));

        // Act
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "a@example.com")
            .QuerySelector("button[data-action='keep']")!.Click();
        ImportCustomersTestDomHelper.FindRowContainingText(cut, ".duplicate-resolution-table tbody tr", "b@example.com")
            .QuerySelector("button[data-action='overwrite']")!.Click();
        cut.Find("button[data-action='confirm-import']").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            (cut.Markup).ShouldContain("Created: 1", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("Updated: 1", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("Skipped: 1", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("Failed: 1", StringComparison.Ordinal);
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

        _ = (createdLink).ShouldNotBeNull();
        _ = (updatedLink).ShouldNotBeNull();
        (createdLink.GetAttribute("href")).ShouldContain($"/customers/{createdId}", StringComparison.OrdinalIgnoreCase);
        (updatedLink.GetAttribute("href")).ShouldContain($"/customers/{updatedId}", StringComparison.OrdinalIgnoreCase);
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

        (cut.FindAll("a[data-action='view-customer']")).ShouldBeEmpty();
        (cut.FindAll("[data-action='customer-id-unavailable']")).ShouldHaveSingleItem();
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

        _ = (link).ShouldNotBeNull();
        (link.GetAttribute("href")).ShouldBe($"/customers/{createdId}");
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
            (cut.Markup).ShouldContain("Failed rows", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("Email is required", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("BirthDate format is invalid", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("bad1@example.com", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("bad2@example.com", StringComparison.Ordinal);
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

        (cells[0]).ShouldBe("3");
        (cells[1]).ShouldBe("-");
        (cells[2]).ShouldBe("Unknown validation error");
        (cells[3]).ShouldBe("-");
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

        _ = (href).ShouldNotBeNull();
        (href).ShouldStartWith("data:text/csv", StringComparison.OrdinalIgnoreCase);
        (download).ShouldBe("import-errors.csv");
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

        _ = (href).ShouldNotBeNull();
        var csvPayload = Uri.UnescapeDataString(href.Split(',', 2)[1]);

        (csvPayload).ShouldContain("LineNumber,Field,Message,Email", StringComparison.Ordinal);
        (csvPayload).ShouldContain("3,,Unknown validation error,", StringComparison.Ordinal);
        (csvPayload).ShouldContain("\"First,Name", StringComparison.Ordinal);
        (csvPayload).ShouldContain("\"Value \"\"quoted\"\"", StringComparison.Ordinal);
        (csvPayload).ShouldContain("bad@example.com", StringComparison.Ordinal);
    }

    [Fact]
    public void Retry_action_after_summary_display_returns_to_mapping_with_previous_file_context()
    {
        var cut = ImportCustomersSummaryTestsHelpers.ConfirmImportWithoutConflicts(this, _fakeCustomersApi, new ImportResultDto(1, 1));

        cut.Find("button[data-action='retry-current-file']").Click();

        cut.WaitForAssertion(() =>
        {
            (cut.Markup).ShouldContain("Source Column (CSV)", StringComparison.Ordinal);
            (cut.Markup).ShouldContain("customers.csv", StringComparison.Ordinal);
        });
    }

}
