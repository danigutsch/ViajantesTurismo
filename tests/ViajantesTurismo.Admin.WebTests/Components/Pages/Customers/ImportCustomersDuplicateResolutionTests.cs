using Microsoft.AspNetCore.Components.Forms;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;
using ViajantesTurismo.Admin.Web.Services;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersDuplicateResolutionTests : BunitContext
{
    private static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(f => f.Name));

    private static readonly string AllCanonicalValues =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(_ => "v"));

    private readonly FakeCustomersApiClient _fakeCustomersApi = new();

    public ImportCustomersDuplicateResolutionTests()
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
    public void ConfirmImport_WithConflicts_AdvancesToDuplicateResolutionStep()
    {
        _fakeCustomersApi.SetImportCustomersResult(new ImportResultDto(0, 0, [new ImportConflictDto("existing@example.com")]));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);

        cut.Find("button.btn-primary").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateResolution_Each_Conflict_Shows_Keep_And_Overwrite_Buttons()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);
        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        var keepButtons = cut.FindAll("button[data-action='keep']");
        var overwriteButtons = cut.FindAll("button[data-action='overwrite']");

        Assert.Equal(2, keepButtons.Count);
        Assert.Equal(2, overwriteButtons.Count);
    }

    [Fact]
    public void DuplicateResolution_ConfirmIsDisabled_UntilAllConflictsResolved()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com"), new ImportConflictDto("b@example.com")]));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);
        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        // No decisions made yet — confirm should be disabled
        Assert.True(cut.Find("button[data-action='confirm-import']").HasAttribute("disabled"));

        // Resolve only one of two — still disabled
        cut.FindAll("button[data-action='keep']")[0].Click();
        Assert.True(cut.Find("button[data-action='confirm-import']").HasAttribute("disabled"));

        // Resolve the second — now enabled
        cut.FindAll("button[data-action='keep']")[1].Click();
        Assert.False(cut.Find("button[data-action='confirm-import']").HasAttribute("disabled"));
    }

    [Fact]
    public void DuplicateResolution_ConfirmImport_ShowsResultAfterAllConflictsResolved()
    {
        _fakeCustomersApi.SetImportCustomersResult(
            new ImportResultDto(0, 0, [new ImportConflictDto("a@example.com")]));
        _fakeCustomersApi.SetCommitImportResult(new ImportResultDto(1, 0));
        var cut = GoToPreview(AllCanonicalHeaders + "\n" + AllCanonicalValues);
        cut.Find("button.btn-primary").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resolve Duplicates", cut.Markup, StringComparison.Ordinal));

        cut.Find("button[data-action='keep']").Click();
        cut.Find("button[data-action='confirm-import']").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("1 customer(s) imported successfully", cut.Markup, StringComparison.Ordinal));
    }
}
