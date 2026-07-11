using AngleSharp.Dom;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

internal static class ImportCustomersSummaryTestsHelpers
{
    public static IRenderedComponent<ImportCustomers> ConfirmImportWithoutConflicts(
        BunitContext context,
        FakeCustomersApiClient fakeCustomersApi,
        ImportResultDto result)
    {
        fakeCustomersApi.SetImportCustomersResult(result);
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(context, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => (cut.Markup).ShouldContain("Import complete.", StringComparison.Ordinal));
        return cut;
    }

    public static IElement FindSuccessSummaryRow(IRenderedComponent<ImportCustomers> cut, string email)
    {
        return ImportCustomersTestDomHelper.FindRowContainingText(cut, "[data-testid='summary-success-rows'] tbody tr", email);
    }
}
