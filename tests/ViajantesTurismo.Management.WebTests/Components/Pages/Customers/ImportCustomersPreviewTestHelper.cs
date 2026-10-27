using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

/// <summary>Drives the import page to its preview state in component tests.</summary>
internal static class ImportCustomersPreviewTestHelper
{
    /// <summary>Renders the import page, uploads the CSV, runs the preview and waits until Confirm Import is enabled.</summary>
    public static IRenderedComponent<ImportCustomers> GoToPreview(
        BunitContext context,
        string csvContent,
        string fileName = "customers.csv")
    {
        var cut = context.Render<ImportCustomers>();
        var file = InputFileContent.CreateFromText(csvContent, fileName);
        cut.FindComponent<InputFile>().UploadFiles(file);
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Preview");
        ImportCustomersTestDomHelper.FindButtonByText(cut, "Preview").Click();
        ImportCustomersTestDomHelper.WaitForEnabledButton(cut, "Confirm Import");
        return cut;
    }
}
