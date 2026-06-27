using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Renders_page_title_and_header()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        var heading = cut.Find("h1");
        Assert.Contains("Import Customers", heading.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_back_to_customers_button()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        var backButton = cut.Find("a.btn-outline-secondary[href='/customers']");
        Assert.Contains("Back to Customers", backButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_description_text()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        Assert.Contains("Upload a CSV file", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_drop_zone_with_instructions_initially()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        Assert.Contains("Drop a CSV file here", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("or click to browse", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_show_any_alert_initially()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        Assert.Empty(cut.FindAll(".alert"));
    }

    [Fact]
    public void Drop_zone_has_secondary_border_initially()
    {
        // Act
        var cut = Render<ImportCustomers>();

        // Assert
        var dropZone = cut.Find(".border.border-2");
        Assert.Contains("border-secondary", dropZone.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Drag_enter_changes_drop_zone_to_primary_border()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var dropZone = cut.Find(".border.border-2");

        // Act
        dropZone.TriggerEvent("ondragenter", EventArgs.Empty);

        // Assert
        dropZone = cut.Find(".border.border-2");
        Assert.Contains("border-primary", dropZone.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Drag_leave_restores_secondary_border()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var dropZone = cut.Find(".border.border-2");
        dropZone.TriggerEvent("ondragenter", EventArgs.Empty);

        // Act
        dropZone.TriggerEvent("ondragleave", EventArgs.Empty);

        // Assert
        dropZone = cut.Find(".border.border-2");
        Assert.Contains("border-secondary", dropZone.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_validation_error_for_non_csv_file()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("data", "report.xlsx");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".alert.alert-danger")));
        Assert.Contains("Only CSV files are supported", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_validation_error_for_oversized_file()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var largeContent = new byte[6 * 1024 * 1024]; // 6 MB exceeds the 5 MB limit
        var file = InputFileContent.CreateFromBinary(largeContent, "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".alert.alert-danger")));
        Assert.Contains("File is too large", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Maximum allowed size is 5 MB", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_success_alert_when_all_rows_imported()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(3, 0));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("3 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("could not be imported", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_warning_alert_when_import_has_row_errors()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(2, 1));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("2 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("1 row(s) could not be imported", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_error_alert_when_api_throws()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersException(new InvalidOperationException("Connection refused"));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("We couldn't import the customers right now. Please try again.", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Connection refused", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Success_result_shows_view_customers_and_import_another_actions()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(1, 0));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act — upload file → mapping step → preview step → confirm import
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Import another file", cut.Markup, StringComparison.Ordinal));
        Assert.NotNull(cut.Find("a[href='/customers']"));
    }

    [Fact]
    public void Reset_restores_drop_zone_after_successful_import()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(1, 0));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => Assert.Contains("Import another file", cut.Markup, StringComparison.Ordinal));

        // Act — click "Import another file" (btn-sm variant in the result alert)
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Import another file").Click();

        // Assert
        Assert.Contains("Drop a CSV file here", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".alert"));
    }
}
