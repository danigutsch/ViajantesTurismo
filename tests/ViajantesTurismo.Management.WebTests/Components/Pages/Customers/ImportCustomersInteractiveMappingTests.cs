using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersInteractiveMappingTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersInteractiveMappingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Shows_dropdown_for_every_defined_field()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert — one select per field in the matcher
        cut.WaitForAssertion(() =>
        {
            var selects = cut.FindAll("select.form-select-sm");
            Assert.Equal(CustomerImportHeaderMatcher.Fields.Count, selects.Count);
        });
    }

    [Fact]
    public void Optional_fields_are_labeled_as_optional()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.Contains("Optional", cut.Markup, StringComparison.Ordinal));
        var optionalCount = CustomerImportHeaderMatcher.Fields.Count(f => !f.IsRequired);
        var optionalBadges = cut.FindAll(".badge.bg-secondary")
            .Where(b => b.TextContent.Trim() == "Optional")
            .ToList();
        Assert.Equal(optionalCount, optionalBadges.Count);
    }

    [Fact]
    public void Auto_matched_field_dropdown_has_CSV_header_preselected()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("Email,FirstName\ntest@test.com,John", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert — the Email dropdown has the "Email" option selected
        cut.WaitForAssertion(() =>
        {
            var emailSelect = cut.Find("select[data-field='Email']");
            var selectedOption = emailSelect.QuerySelector("option[selected]");
            Assert.NotNull(selectedOption);
            Assert.Equal("Email", selectedOption.GetAttribute("value"));
        });
    }

    [Fact]
    public void Clearing_required_auto_matched_field_via_dropdown_disables_import()
    {
        // Arrange — all canonical headers: all required fields matched → import enabled
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");

        // Act — explicitly clear a required field dropdown (overrides auto-match)
        cut.Find("select[data-field='Email']").Change("");

        // Assert — import disabled because required field is now unassigned
        Assert.True(ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled"));
    }

    [Fact]
    public void Clearing_optional_field_does_not_disable_import()
    {
        // Arrange — all canonical headers present: all required matched, import enabled
        const string optionalFieldName = "Instagram";
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(CustomerImportCsvTestData.AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");

        // Act — clear an optional field
        cut.Find($"select[data-field='{optionalFieldName}']").Change("");

        // Assert — import still enabled (optional field not required)
        Assert.False(ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled"));
    }
}
