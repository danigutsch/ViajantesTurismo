using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersHeaderMappingTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersHeaderMappingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void After_file_selected_shows_header_mapping_step()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".card-header")));
        Assert.Contains("customers.csv", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void All_canonical_headers_show_all_required_matched_alert()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".alert-success")));
        Assert.Contains("All required columns were automatically matched", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_headers_show_warning_and_required_field_dropdowns()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("Col1,Col2,Col3\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".alert-warning")));
        Assert.NotEmpty(cut.FindAll("select.form-select-sm"));
    }

    [Fact]
    public void Warning_message_states_count_of_unmatched_required_fields()
    {
        // Arrange — provide only Email so all other required fields are unmatched
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("Email\ntest@test.com", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".alert-warning")));
        var requiredCount = CustomerImportHeaderMatcher.Fields.Count(f => f.IsRequired) - 1; // -1 for Email
        Assert.Contains($"{requiredCount} required column(s) could not be matched", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Import_button_disabled_when_required_fields_unmatched()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("UnknownCol\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotNull(ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview")));
        Assert.True(ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled"));
    }

    [Fact]
    public void Import_button_enabled_when_all_required_headers_auto_matched()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotNull(ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview")));
        Assert.False(ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled"));
    }

    [Fact]
    public void Choose_different_file_returns_to_file_selection_step()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".card-header")));

        // Act
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Choose different file").Click();

        // Assert
        Assert.Contains("Drop a CSV file here", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void File_name_shown_in_mapping_card_header()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "my-import.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".card-header")));
        Assert.Contains("my-import.csv", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Detected_column_count_shown_in_mapping_card_header()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("FirstName,LastName,Email\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".card-header")));
        Assert.Contains("3 column(s) detected", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Selecting_column_for_required_field_enables_import_when_all_assigned()
    {
        // Arrange — only provide required fields via a single unknown column to test one-field scenario
        // Use canonical headers minus one required field, plus a custom column for that field
        const string requiredFieldName = "FirstName";
        var otherRequiredHeaders = CustomerImportHeaderMatcher.Fields
            .Where(f => f.IsRequired && f.Name != requiredFieldName)
            .Select(f => f.Name);
        var headers = string.Join(",", otherRequiredHeaders.Append("CustomCol"));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(headers + "\ndata", "customers.csv");

        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("select.form-select-sm")));

        // Import should be disabled before assignment
        Assert.True(ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled"));

        // Act — assign CustomCol to the unmatched required field
        cut.Find($"select[data-field='{requiredFieldName}']").Change("CustomCol");

        // Assert — Import enabled now
        Assert.False(ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled"));
    }
}
