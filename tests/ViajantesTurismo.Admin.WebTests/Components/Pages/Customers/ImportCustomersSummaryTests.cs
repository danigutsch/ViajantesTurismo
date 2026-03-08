using Microsoft.AspNetCore.Components.Forms;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;
using ViajantesTurismo.Admin.Web.Services;

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
        cut.WaitForAssertion(() => Assert.False(cut.Find("button.btn-primary").HasAttribute("disabled")));
        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Confirm Import", cut.Markup, StringComparison.Ordinal));
        return cut;
    }

    [Fact]
    public void ConfirmImport_AfterDuplicateDecisions_Shows_Created_Updated_Skipped_And_Failed_Counts()
    {
        // Arrange
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(2, 1));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);
        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        // Act
        cut.FindAll("button[data-action='keep']")[0].Click();
        cut.FindAll("button[data-action='overwrite']")[1].Click();
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
}
