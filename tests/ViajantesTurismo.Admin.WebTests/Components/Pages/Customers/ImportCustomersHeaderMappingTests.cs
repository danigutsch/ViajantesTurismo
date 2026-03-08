using Microsoft.AspNetCore.Components.Forms;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;
using ViajantesTurismo.Admin.Web.Services;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersHeaderMappingTests : BunitContext
{
    private static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(f => f.Name));

    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersHeaderMappingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void After_File_Selected_Shows_Header_Mapping_Step()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".card-header")));
        Assert.Contains("customers.csv", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void All_Canonical_Headers_Show_All_Required_Matched_Alert()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".alert-success")));
        Assert.Contains("All required columns were automatically matched", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_Headers_Show_Warning_And_Required_Field_Dropdowns()
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
    public void Warning_Message_States_Count_Of_Unmatched_Required_Fields()
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
    public void Import_Button_Disabled_When_Required_Fields_Unmatched()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("UnknownCol\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button.btn-primary")));
        Assert.True(cut.Find("button.btn-primary").HasAttribute("disabled"));
    }

    [Fact]
    public void Import_Button_Enabled_When_All_Required_Headers_Auto_Matched()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button.btn-primary")));
        Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled"));
    }

    [Fact]
    public void Choose_Different_File_Returns_To_File_Selection_Step()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".card-header")));

        // Act
        cut.Find("button.btn-outline-secondary").Click();

        // Assert
        Assert.Contains("Drop a CSV file here", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void File_Name_Shown_In_Mapping_Card_Header()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "my-import.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".card-header")));
        Assert.Contains("my-import.csv", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Detected_Column_Count_Shown_In_Mapping_Card_Header()
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
    public void Auto_Matched_Column_Count_Shown_In_Details_Summary()
    {
        // Arrange
        var totalFields = CustomerImportHeaderMatcher.Fields.Count;
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(AllCanonicalHeaders + "\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("details")));
        Assert.Contains($"{totalFields} auto-matched column(s)", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Partial_Match_Shows_Only_Unmatched_Required_Dropdowns_Not_Matched_Ones()
    {
        // Arrange — provide some required headers so only some are unmatched
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("FirstName,LastName,Email\ndata", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert: dropdowns shown only for fields NOT in the file
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("select.form-select-sm")));
        var selects = cut.FindAll("select.form-select-sm");
        var unmatchedRequired = CustomerImportHeaderMatcher.Fields
            .Count(f => f.IsRequired && f.Name != "FirstName" && f.Name != "LastName" && f.Name != "Email");
        Assert.Equal(unmatchedRequired, selects.Count);
    }

    [Fact]
    public void Selecting_Column_For_Required_Field_Enables_Import_When_All_Assigned()
    {
        // Arrange — only provide required fields via a single unknown column to test one-field scenario
        // Use canonical headers minus one required field, plus a custom column for that field
        var requiredField = CustomerImportHeaderMatcher.Fields.First(f => f.IsRequired);
        var otherRequiredHeaders = CustomerImportHeaderMatcher.Fields
            .Where(f => f.IsRequired && f.Name != requiredField.Name)
            .Select(f => f.Name);
        var headers = string.Join(",", otherRequiredHeaders.Append("CustomCol"));
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(headers + "\ndata", "customers.csv");

        cut.FindComponent<InputFile>().UploadFiles(file);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("select.form-select-sm")));

        // Import should be disabled before assignment
        Assert.True(cut.Find("button.btn-primary").HasAttribute("disabled"));

        // Act — assign CustomCol to the unmatched required field
        cut.Find("select.form-select-sm").Change("CustomCol");

        // Assert — Import enabled now
        Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled"));
    }
}
