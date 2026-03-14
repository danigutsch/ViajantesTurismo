using Microsoft.AspNetCore.Components.Forms;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared.Fakes.ApiClients;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;
using ViajantesTurismo.Admin.Web.Services;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersInteractiveMappingTests : BunitContext
{
    private static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(f => f.Name));

    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersInteractiveMappingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Shows_Dropdown_For_Every_Defined_Field()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

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
    public void Optional_Fields_Are_Labeled_As_Optional()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

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
    public void Auto_Matched_Field_Dropdown_Has_CSV_Header_Preselected()
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
    public void Clearing_Required_Auto_Matched_Field_Via_Dropdown_Disables_Import()
    {
        // Arrange — all canonical headers: all required fields matched → import enabled
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));

        // Act — explicitly clear a required field dropdown (overrides auto-match)
        cut.Find("select[data-field='Email']").Change("");

        // Assert — import disabled because required field is now unassigned
        Assert.True(cut.Find("button.btn-primary").HasAttribute("disabled"));
    }

    [Fact]
    public void Clearing_Optional_Field_Does_Not_Disable_Import()
    {
        // Arrange — all canonical headers present: all required matched, import enabled
        var optionalField = CustomerImportHeaderMatcher.Fields.First(f => !f.IsRequired);
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));

        // Act — clear an optional field
        cut.Find($"select[data-field='{optionalField.Name}']").Change("");

        // Assert — import still enabled (optional field not required)
        Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled"));
    }
}
