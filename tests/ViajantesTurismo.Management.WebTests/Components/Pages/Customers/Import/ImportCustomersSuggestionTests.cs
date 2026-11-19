using System.Text;
using PandaBear.Blazor;
using PandaBear.Testing;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Import;

[TestScope(TestScope.Component)]
public sealed class ImportCustomersSuggestionTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersSuggestionTests()
    {
        Services.AddUiFailureReporting();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Recommends_a_translated_header_and_submits_the_confirmed_column()
    {
        // Arrange
        string? submittedCsv = null;
        _fakeCustomersApi.SetImportCustomersHandler((content, _, _, _) =>
        {
            submittedCsv = Encoding.UTF8.GetString(content.Span);
            return Task.FromResult(new ImportResultDto(1, 0));
        });
        var headers = CustomerImportCsvTestData.AllCanonicalHeaders.Replace("FirstName", "Nome", StringComparison.Ordinal);
        var file = InputFileContent.CreateFromText(headers + "\n" + CustomerImportCsvTestData.AllCanonicalValues, "customers.csv");
        var cut = Render<ImportCustomers>();

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => cut.Find("#FirstName-mapping-help").TextContent.ShouldContain("Suggested column Nome", StringComparison.Ordinal));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled").ShouldBeTrue();
        submittedCsv.ShouldBeNull();

        // Act
        cut.Find("select[data-field='FirstName']").Change("Nome");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("1 customer(s) imported successfully", StringComparison.Ordinal));
        submittedCsv.ShouldNotBeNull().ShouldStartWith(CustomerImportCsvTestData.AllCanonicalHeaders, StringComparison.Ordinal);
    }

    [Fact]
    public void Recognizes_email_shape_without_displaying_sample_values_during_mapping()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var rows = ",Alice\n" + string.Join("\n", Enumerable.Repeat("example@example.test,Sam", 4)) + "\nnot-an-email,Alex";
        var file = InputFileContent.CreateFromText("ContactRef,FirstName\n" + rows, "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => cut.Find("#Email-mapping-help").TextContent.ShouldContain("Suggested column ContactRef", StringComparison.Ordinal));
        cut.Markup.ShouldContain("Sample values look like email addresses.", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("example@example.test", StringComparison.Ordinal);
    }

    [Fact]
    public void Conflicting_email_headers_require_a_source_choice()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var headers = CustomerImportCsvTestData.AllCanonicalHeaders + ",E-mail";
        var values = CustomerImportCsvTestData.AllCanonicalValues + ",other@example.test";
        var file = InputFileContent.CreateFromText(headers + "\n" + values, "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Multiple possible columns", StringComparison.Ordinal));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled").ShouldBeTrue();

        // Act
        cut.Find("select[data-field='Email']").Change("E-mail");

        // Assert
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Ambiguous_optional_fields_can_be_explicitly_left_unmapped()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var headers = CustomerImportCsvTestData.AllCanonicalHeaders + ",Bairro";
        var values = CustomerImportCsvTestData.AllCanonicalValues + ",Centro";
        var file = InputFileContent.CreateFromText(headers + "\n" + values, "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => cut.Find("#Neighborhood-mapping-help").TextContent
            .ShouldContain("Multiple possible columns", StringComparison.Ordinal));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled").ShouldBeTrue();

        // Act
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Leave unmapped").Click();

        // Assert
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void One_source_column_cannot_be_assigned_to_two_fields()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(
            CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues, "customers.csv");
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");

        // Act
        cut.Find("select[data-field='FirstName']").Change("LastName");

        // Assert
        cut.Markup.ShouldContain("Use each source column only once.", StringComparison.Ordinal);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Identifies_the_required_field_without_a_viable_suggestion()
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText("City,FirstName\nRecife,Alice", "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => cut.Find("[data-missing-required-field='Email']").TextContent
            .ShouldContain("No suggested source", StringComparison.Ordinal));
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").HasAttribute("disabled").ShouldBeTrue();
    }

    [Theory]
    [InlineData("FirstName,Email\nAlice", "row column count")]
    [InlineData("Email,email\nfirst@example.test,second@example.test", "Column names must be unique")]
    [InlineData("FirstName,Email", "at least one data row")]
    [InlineData("FirstName,\nAlice,example@example.test", "Each column needs a name")]
    [InlineData("FirstName,Email\n\"Alice,example@example.test", "quoted field is not closed")]
    public void Explains_why_a_structurally_invalid_file_cannot_be_imported(string csv, string expectedMessage)
    {
        // Arrange
        var cut = Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(csv, "customers.csv");

        // Act
        cut.FindComponent<InputFile>().UploadFiles(file);

        // Assert
        cut.WaitForAssertion(() => cut.Find(".alert-danger").TextContent.ShouldContain(expectedMessage, StringComparison.Ordinal));
        cut.FindAll("select[data-field]").ShouldBeEmpty();
    }
}
