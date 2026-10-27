using AngleSharp.Dom;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

/// <summary>Helpers for import summary component tests.</summary>
internal static class ImportCustomersSummaryTestsHelpers
{
    /// <summary>Imports a canonical CSV with the given result configured on the fake API and waits for the completion message.</summary>
    public static IRenderedComponent<ImportCustomers> ConfirmImportWithoutConflicts(
        BunitContext context,
        FakeCustomersApiClient fakeCustomersApi,
        ImportResultDto result)
    {
        fakeCustomersApi.SetImportCustomersResult(result);
        var cut = ImportCustomersPreviewTestHelper.GoToPreview(context, CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.AllCanonicalValues);
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Confirm Import").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Import complete.", StringComparison.Ordinal));
        return cut;
    }

    /// <summary>Finds the success summary row containing the given email.</summary>
    public static IElement FindSuccessSummaryRow(IRenderedComponent<ImportCustomers> cut, string email)
    {
        return ImportCustomersTestDomHelper.FindRowContainingText(cut, "[data-testid='summary-success-rows'] tbody tr", email);
    }
}
